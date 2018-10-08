﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Convex.Clients.Services;
using Convex.IRC.Models;
using Microsoft.AspNetCore.Mvc;

namespace Convex.Clients.Controllers {
    [Route("api/[controller]")]
    public class IrcController : Controller {
        #region MEMBERS

        private ClientService ClientService { get; }
        private IrcService IrcClientReference { get; }

        #endregion

        public IrcController(ClientService clientService, IrcService service) {
            IrcClientReference = service;
            ClientService = clientService;
        }

        //GET api/irc
        [HttpGet]
        public List<ServerMessage> Get(string guid, double unixTimestamp) {
            if (!Guid.TryParse(guid, out Guid clientGuid))
                throw new HttpRequestException("Guid must be in proper format.");

            if (!ClientService.IsClientVerified(clientGuid))
                throw new HttpRequestException("Client must be verified.");

            DateTime referenceTime = new DateTime(1970, 01, 01, 00, 00, 00, 00, DateTimeKind.Utc);
            referenceTime = referenceTime.AddSeconds(unixTimestamp).ToLocalTime();

            // DateTime.MinValue == 1/1/0001 12:00:00 AM (YYYYMMDDhhmmss 1970,01,01,00,00,00,00)
            return IrcClientReference.GetMessagesByDateTimeOrDefault(referenceTime, DateTimeOrdinal.After).ToList();
        }
    }
}
