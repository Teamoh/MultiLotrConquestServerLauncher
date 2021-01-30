using System.Collections.Generic;
using System.Diagnostics;

namespace MultiLotrConquestServerLauncher {
    class ServerObserver {

        #region Properties

        private readonly Logger _logger;
        private readonly string _serverProcessName;

        #endregion

        #region Constructor

        public ServerObserver() {
            _logger = Logger.GetInstance();
            _serverProcessName = Config.ServerProcessName;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a list containg
        /// the server objects
        /// whose process isnt active anymore
        /// (probably crashed)
        /// </summary>
        /// <param name="servers">The expected servers to run</param>
        /// <returns>List containing crashed server objects</returns>
        public List<LotrcServer> DetectDownServers(List<LotrcServer> servers) {
            var downServers = new List<LotrcServer>();
            var serverProcesses = Process.GetProcessesByName(_serverProcessName);

            foreach (var server in servers) {
                bool foundMatchingProcess = false;

                foreach (var serverProcess in serverProcesses) {
                    if (serverProcess.Id == server.ProcessId) {
                        foundMatchingProcess = true;
                        break;
                    }
                }

                if (!foundMatchingProcess) {
                    downServers.Add(server);
                }
            }

            return downServers;
        }

        /// <summary>
        /// Replaces the down servers
        /// in the original servers list
        /// by comparing it to the
        /// relaunched servers
        /// </summary>
        /// <param name="servers">Stored server list</param>
        /// <param name="relaunchedServers">List of relaunched servers</param>
        /// <returns>A new list containg the objects of all currently running servers</returns>
        public List<LotrcServer> ReplaceDownServers(List<LotrcServer> servers, List<LotrcServer> relaunchedServers) {
            var resultServerList = new List<LotrcServer>(servers);

            foreach (var relaunchedServer in relaunchedServers) {
                // find the crashed server by the name of the dedicated file
                // and replace it with the freshly re-launched server
                // in the server list
                var counter = 0;

                foreach (var server in servers) {
                    if (server.DedicatedItem.FileName == relaunchedServer.DedicatedItem.FileName) {
                        resultServerList[counter] = relaunchedServer; // replace crashed server with re-launched server
                    }

                    counter++;
                }
            }

            return resultServerList;
        }

        #endregion
    }
}
