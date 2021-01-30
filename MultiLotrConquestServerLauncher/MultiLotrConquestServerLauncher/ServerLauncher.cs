using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace MultiLotrConquestServerLauncher {
    class ServerLauncher {

        #region Properties

        private readonly Logger _logger;
        private readonly MultiServerConfig _multiServerConfig;
        private readonly string _documentsServerDirectoryPath;
        private readonly string _targetFileName;
        private readonly string _targetFilePath;
        private readonly string _serverFilePath;
        private readonly string _serverFileDirectory;
        private readonly string _serverFileName;
        private readonly int _serverLaunchTimeoutMs;

        #endregion

        #region Constructor

        public ServerLauncher(MultiServerConfig multiServerConfig) {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var documentsServerDirectoryName = Config.DocumentsServerDirectoryName;

            _logger = Logger.GetInstance();
            _multiServerConfig = multiServerConfig;
            _documentsServerDirectoryPath = documentsPath + Path.DirectorySeparatorChar + documentsServerDirectoryName;
            _targetFileName = multiServerConfig.TargetFileName;
            _targetFilePath = _documentsServerDirectoryPath + Path.DirectorySeparatorChar + _targetFileName;
            _serverFilePath = _multiServerConfig.ServerFilePath;
            _serverFileDirectory = Path.GetDirectoryName(_serverFilePath);
            _serverFileName = Path.GetFileName(_serverFilePath);
            _serverLaunchTimeoutMs = Config.ServerLaunchTimeoutMs;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the current directory
        /// to the directory where the LOTRC
        /// server file is located
        /// </summary>
        public void SetCurrentDirectory() {
            var currentDirectory = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(_serverFileDirectory) && _serverFileDirectory != currentDirectory) {
                // change current directory to the directory where the ConquestServer.exe is, otherwise the lotrc server will crash
                Directory.SetCurrentDirectory(_serverFileDirectory);
                _logger.Info($"Current directory was changed to '{_serverFileDirectory}'");
            } else {
                _logger.Info($"Current directory was not changed");
            }
        }

        /// <summary>
        /// Launches all configured
        /// LOTR conquest servers
        /// and returns a list of
        /// lotrc-server objects
        /// </summary>
        public List<LotrcServer> LaunchAllServers() {
            var dedicatedItems = _multiServerConfig.DedicatedItems;
            var servers = LaunchSelectedServers(dedicatedItems);
            return servers;
        }

        /// <summary>
        /// Launches a list of servers
        /// </summary>
        /// <param name="dedicatedItems">The list of dedicated items</param>
        /// <returns>List of launched servers</returns>
        public List<LotrcServer> LaunchSelectedServers(List<DedicatedItem> dedicatedItems) {
            var serverCounter = 0;
            var servers = new List<LotrcServer>();

            foreach (var dedicatedItem in dedicatedItems) {
                serverCounter++;

                var dedicatedFileName = dedicatedItem.FileName;
                _logger.Info($"Launching server {serverCounter}/{dedicatedItems.Count} ({dedicatedFileName})...");

                var server = LaunchSingleServer(dedicatedItem);

                if (server == null) {
                    continue;
                }

                servers.Add(server);

                _logger.Debug($"Waiting for server launch...");
                Thread.Sleep(_serverLaunchTimeoutMs);
            }

            return servers;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Launches a single server
        /// </summary>
        /// <param name="dedicatedItem">The dedicated item</param>
        /// <returns>The server object or null in error cases</returns>
        private LotrcServer LaunchSingleServer(DedicatedItem dedicatedItem) {
            var dedicatedFileName = dedicatedItem.FileName;
            var dedicatedFilePath = _documentsServerDirectoryPath + Path.DirectorySeparatorChar + dedicatedFileName;

            if (!File.Exists(dedicatedFilePath)) {
                _logger.Warn($"Filtered out not existent dedicated file '{dedicatedFileName}'");
                return null;
            }

            // TODO: add logic to randomize level order/start level

            var dedicatedXml = new XmlDocument();

            try {
                dedicatedXml.Load(dedicatedFilePath);
            } catch (Exception exc) {
                _logger.Error($"Failed to load '{dedicatedFilePath}': {exc.Message}");
                return null;
            }

            ModifyDedicatedXml(dedicatedXml, dedicatedItem);

            var fileContent = dedicatedXml.OuterXml;
            // var fileContent = File.ReadAllText(dedicatedFilePath);

            try {
                File.WriteAllText(_targetFilePath, fileContent);
            } catch (Exception exc) {
                _logger.Error($"Failed to write content of '{dedicatedFileName}' into '{_targetFileName}': {exc.Message}");
                return null;
            }

            _logger.Debug($"Wrote content of '{dedicatedFileName}' into '{_targetFileName}'");

            Process process;

            try {
                process = Process.Start(_serverFileName); // start ConquestServer.exe
            } catch (Exception exception) {
                _logger.Error($"Failed to launch server ('{_serverFileName}' in directory '{_serverFilePath}'). Please make sure that this application is inside the same directory as the ConquestServer.exe. Error: {exception.Message}");
                return null;
            }

            var server = new LotrcServer {
                ProcessId = process.Id,
                DedicatedItem = dedicatedItem
            };

            return server;
        }

        /// <summary>
        /// Modifies the dedicated XML document
        /// with the configured options of
        /// the dedicatedItem
        /// </summary>
        /// <param name="dedicatedXml">The dedicated XML document</param>
        /// <param name="dedicatedItem">The dedicated item</param>
        private void ModifyDedicatedXml(XmlDocument dedicatedXml, DedicatedItem dedicatedItem) {
            if (dedicatedItem.Port > 0) {
                _logger.Debug("Writing port...");
                WritePort(dedicatedXml, dedicatedItem.Port);
            }

            if (!string.IsNullOrWhiteSpace(dedicatedItem.GameName)) {
                _logger.Debug("Writing game name...");
                WriteGameName(dedicatedXml, dedicatedItem.GameName);
            }

            if (dedicatedItem.RandomLevelOrder) {
                _logger.Debug("Randomizing level order...");
                RandomizeLevelOrder(dedicatedXml);
            } else if (dedicatedItem.RandomStartLevel) {
                _logger.Debug("Randomizing start level...");
                RandomizeStartLevel(dedicatedXml);
            }
        }

        /// <summary>
        /// Writes the port to the dedicated XML document
        /// </summary>
        /// <param name="dedicatedXml">The dedicated XML document</param>
        /// <param name="port">The port number</param>
        private void WritePort(XmlDocument dedicatedXml, int port) {
            WriteNode(dedicatedXml, "Plasma", "Port", port.ToString());
        }

        /// <summary>
        /// Writes the gameName to the dedicated XML document
        /// </summary>
        /// <param name="dedicatedXml">The dedicated XML document</param>
        /// <param name="gameName">The game name</param>
        private void WriteGameName(XmlDocument dedicatedXml, string gameName) {
            WriteNode(dedicatedXml, "Plasma", "GameName", gameName);
        }

        /// <summary>
        /// Writes the value of a node inside the main node
        /// (e. g. 'Plasma', 'Network', 'Session').
        /// If the main or sub node doesnt exist, they will be created.
        /// </summary>
        /// <param name="nodeName">The name of the node</param>
        /// <param name="mainNodeName">The name of the main node</param>
        /// <param name="value">The value of the node which wil be set as innerText</param>
        private void WriteNode(XmlDocument dedicatedXml, string mainNodeName, string nodeName, string value) {
            var configNode = dedicatedXml.SelectSingleNode("/Config");
            var mainNode = configNode.SelectSingleNode($"./{mainNodeName}");

            if (mainNode == null) {
                mainNode = dedicatedXml.CreateElement(mainNodeName);
                configNode.AppendChild(mainNode);
            }

            var node = mainNode.SelectSingleNode($"./{nodeName}");

            if (node == null) {
                node = dedicatedXml.CreateElement(nodeName);
                mainNode.AppendChild(node);
            }

            node.InnerText = value;
        }

        /// <summary>
        /// Randomizes the order of the Level nodes
        /// </summary>
        /// <param name="dedicatedXml">The dedicated XML document</param>
        private void RandomizeLevelOrder(XmlDocument dedicatedXml) {
            var levelsNode = dedicatedXml.SelectSingleNode("/Config/Levels");
            var levelNodes = levelsNode.SelectNodes("./Level");
            var levelNodesList = levelNodes.Cast<XmlNode>().ToList();
            var rnd = new Random();
            var randomizedLevelNodesList = levelNodesList
                .Select(levelNode => new { levelNode, order = rnd.Next() })
                .OrderBy(x => x.order)
                .Select(x => x.levelNode).ToList();

            foreach (XmlNode levelNode in randomizedLevelNodesList) {
                // append the randomly sorted levels to the levels node
                levelsNode.AppendChild(levelNode);
            }
        }

        /// <summary>
        /// Randomizes the start level only and 
        /// keeps the following level order
        /// </summary>
        /// <param name="dedicatedXml">The dedicated XML document</param>
        private void RandomizeStartLevel(XmlDocument dedicatedXml) {
            var levelsNode = dedicatedXml.SelectSingleNode("/Config/Levels");
            var levelNodes = levelsNode.SelectNodes("./Level");
            var rnd = new Random();
            var startLevelIndex = rnd.Next(0, levelNodes.Count);

            // append all levels coming before the random start level to the end of the levels node
            for (int i = 0; i < startLevelIndex; i++) {
                levelsNode.AppendChild(levelNodes[i]);
            }
        }

        #endregion
    }
}
