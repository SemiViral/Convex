﻿#region usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convex.Event;
using Convex.IRC.ComponentModel;
using Microsoft.Data.Sqlite;

#endregion

namespace Convex.IRC.Models {
    public class Database {
        #region MEMBERS

        internal string FilePath { get; }

        public bool Connected { get; private set; }
        public bool IsInitialised { get; private set; }
        public readonly ObservableCollection<User> Users;

        #endregion

        /// <summary>
        ///     Initialise connections to database and sets properties
        /// </summary>
        public Database(string databaseFilePath) {
            Users = new ObservableCollection<User>();
            Users.CollectionChanged += UserAdded;

            FilePath = databaseFilePath;
        }

        #region INIT

        public async Task Initialise() {
            await CheckCreate();

            foreach (User user in await LoadUsers())
                Users.Add(user);

            IsInitialised = true;
            await OnInitialised(this, new ClassInitialisedEventArgs(this));
        }

        private async Task CheckCreate() {
            if (File.Exists(FilePath))
                return;

            await LogInformation(this, new InformationLoggedEventArgs("Database file not found, creating."));

            using (SqliteConnection connection = GetConnection(FilePath, SqliteOpenMode.ReadWriteCreate)) {
                connection.Open();
                await connection.QueryAsync(new DatabaseQueriedEventArgs("CREATE TABLE IF NOT EXISTS users (id int, nickname string, realname string, access int, seen string)"));
                await connection.QueryAsync(new DatabaseQueriedEventArgs("CREATE TABLE IF NOT EXISTS messages (id int, sender string, message string, datetime string)"));
            }

            Connected = true;
        }

        private static SqliteConnection GetConnection(string source, SqliteOpenMode mode = SqliteOpenMode.ReadWrite) {
            return new SqliteConnection(new SqliteConnectionStringBuilder {DataSource = source, Mode = mode}.ToString());
        }

        private async Task<IEnumerable<User>> LoadUsers() {
            await LogInformation(this, new InformationLoggedEventArgs("Loading users."));

            List<User> users = new List<User>();

            using (SqliteConnection connection = GetConnection(FilePath)) {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction()) {
                    SqliteCommand getUsers = connection.CreateCommand();
                    getUsers.Transaction = transaction;
                    getUsers.CommandText = "SELECT * FROM users";

                    using (SqliteDataReader userEntries = getUsers.ExecuteReader()) {
                        while (userEntries.Read()) {
                            int id = Convert.ToInt32(userEntries.GetValue(0));
                            string nickname = userEntries.GetValue(1).ToString();
                            string realname = (string)userEntries.GetValue(2);
                            int access = Convert.ToInt32(userEntries.GetValue(3));
                            DateTime seen = DateTime.Parse((string)userEntries.GetValue(4));

                            users.Add(new User(id, nickname, realname, access));
                        }
                    }

                    transaction.Commit();
                }
            }

            ReadMessagesIntoUsers();

            return users;
        }

        #endregion

        #region METHODS

        internal int GetLastDatabaseId() {
            int id;

            using (SqliteConnection connection = GetConnection(FilePath)) {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction()) {
                    SqliteCommand getId = connection.CreateCommand();
                    getId.Transaction = transaction;
                    getId.CommandText = "SELECT MAX(id) FROM users";
                    id = (int)getId.ExecuteScalar();

                    transaction.Commit();
                }
            }

            return id;
        }

        private void ReadMessagesIntoUsers() {
            if (Users.Count.Equals(0))
                return;

            using (SqliteConnection connection = GetConnection(FilePath)) {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction()) {
                    SqliteCommand getMessages = connection.CreateCommand();
                    getMessages.Transaction = transaction;
                    getMessages.CommandText = "SELECT * FROM messages";

                    using (SqliteDataReader messages = getMessages.ExecuteReader()) {
                        while (messages.Read())
                            Users.SingleOrDefault(e => e.Id.Equals(Convert.ToInt32(messages["id"])))?.Messages.Add(new Message((int)messages["id"], (string)messages["sender"], (string)messages["message"], DateTime.Parse((string)messages["datetime"])));
                    }

                    transaction.Commit();
                }
            }
        }

        public bool UserExists(string realname) {
            bool exists;

            using (SqliteConnection connection = GetConnection(FilePath)) {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction()) {
                    SqliteCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.Parameters.AddWithValue("@user", realname);
                    command.CommandText = "SELECT COUNT(*) FROM users WHERE ([realname] = @user)";

                    exists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }

            return exists;
        }

        /// <summary>
        ///     Creates a new user and updates the users & userTimeouts collections
        /// </summary>
        /// <param name="access">access level of user</param>
        /// <param name="nickname">nickname of user</param>
        /// <param name="realname">realname of user</param>
        /// <param name="seen">last time user was seen</param>
        internal async Task CreateUserAsync(int access, string nickname, string realname, DateTime seen) {
            Debug.WriteLine($"Creating database entry for {realname}.");

            await GetConnection(FilePath).QueryAsync(new DatabaseQueriedEventArgs($"INSERT INTO users VALUES ({GetLastDatabaseId() + 1}, '{nickname}', '{realname}', {access}, '{seen}')"));
        }

        internal void CreateUser(int access, string nickname, string realname, DateTime seen) {
            Debug.WriteLine($"Creating database entry for {realname}.");

            GetConnection(FilePath).Query(new DatabaseQueriedEventArgs($"INSERT INTO users VALUES ({GetLastDatabaseId() + 1}, '{nickname}', '{realname}', {access}, '{seen}')"));
        }

        #endregion

        #region EVENTS

        public event AsyncEventHandler<ClassInitialisedEventArgs> Initialised;
        public event AsyncEventHandler<InformationLoggedEventArgs> Log;

        private async Task OnInitialised(object sender, ClassInitialisedEventArgs args) {
            if (Initialised == null)
                return;

            await Initialised.Invoke(sender, args);
        }

        private async Task LogInformation(object sender, InformationLoggedEventArgs args) {
            if (Log == null)
                return;

            await Log.Invoke(sender, args);
        }

        private void UserAdded(object sender, NotifyCollectionChangedEventArgs args) {
            if (!args.Action.Equals(NotifyCollectionChangedAction.Add))
                return;

            foreach (object item in args.NewItems) {
                if (!(item is User))
                    continue;

                if (!UserExists(((User)item).Realname)) {
                    User newUser = (User)item;
                    CreateUser(newUser.Access, newUser.Nickname, newUser.Realname, newUser.Seen);
                }

                ((User)item).PropertyChanged += AutoUpdateUsers;
                ((User)item).Messages.CollectionChanged += MessageAdded;
            }
        }

        private void MessageAdded(object sender, NotifyCollectionChangedEventArgs args) {
            if (!args.Action.Equals(NotifyCollectionChangedAction.Add))
                return;

            foreach (object item in args.NewItems) {
                if (!(item is Message))
                    continue;

                Message message = (Message)item;

                GetConnection(FilePath).Query(new DatabaseQueriedEventArgs($"INSERT INTO messages VALUES ({message.Id}, '{message.Sender}', '{message.Contents}', '{message.Date}')"));
            }
        }

        private void AutoUpdateUsers(object sender, PropertyChangedEventArgs args) {
            if (!(args is UserPropertyChangedEventArgs))
                return;

            UserPropertyChangedEventArgs castedArgs = (UserPropertyChangedEventArgs)args;

            GetConnection(FilePath).Query(new DatabaseQueriedEventArgs($"UPDATE users SET {castedArgs.PropertyName}='{castedArgs.NewValue}' WHERE realname='{castedArgs.Name}'"));
        }

        #endregion
    }
}
