using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace MultiLotrConquestServerLauncher {
    class Program {
        static void Main(string[] args) {

            #region Variables

            var documentsServerDirectoryName = Config.DocumentsServerDirectoryName;
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var documentsServerDirectoryPath = documentsPath + Path.DirectorySeparatorChar + documentsServerDirectoryName;
            var logFileName = Config.LogFileName;
            var logFilePath = documentsServerDirectoryPath + Path.DirectorySeparatorChar + logFileName;
            var logger = Logger.GetInstance(logFilePath);
            var currentDirectory = Directory.GetCurrentDirectory();
            var multiServerConfigFileName = Config.MultiServerConfigFileName;
            var multiServerConfigFilePath = documentsPath + Path.DirectorySeparatorChar + documentsServerDirectoryName + Path.DirectorySeparatorChar + multiServerConfigFileName;
            var multiServerConfig = new MultiServerConfig(multiServerConfigFilePath);

            #endregion

            #region Read Config

            try {
                multiServerConfig.Read();
            } catch (Exception exc) {
                logger.Fatal($"Failed to read multi server config file at '{multiServerConfigFilePath}': {exc.Message}");
                return;
            }

            #endregion

            #region Set Log Level

            var configuredLogLevel = multiServerConfig.LogLevel;
            logger.Info($"Setting log level to {configuredLogLevel}");
            logger.SetLogLevel(configuredLogLevel);

            #endregion

            #region Server Launch

            var serverLauncher = new ServerLauncher(multiServerConfig);
            var serverObserver = new ServerObserver();
            var serverDownCheckIntervalMs = Config.ServerLaunchTimeoutMs;

            logger.Info("Started MultiLotrConquestServerLauncher");
            logger.Info($"Current directory: '{currentDirectory}'");

            logger.Debug($"Setting current directory");
            serverLauncher.SetCurrentDirectory();

            List<LotrcServer> serverList;

            try {
                logger.Info("Launching configured servers...");
                serverList = serverLauncher.LaunchAllServers();
                logger.Info("Successfully launched configured servers");
            } catch (Exception exc) {
                logger.Fatal($"Failed to initially launch servers: {exc.Message}");
                return;
            }

            #endregion

            #region Server Observing

            while (true) {
                logger.Debug($"Waiting to check for down servers...");

                Thread.Sleep(serverDownCheckIntervalMs);

                logger.Debug($"Checking for down servers...");

                var downServers = serverObserver.DetectDownServers(serverList);
                var downServersCount = downServers.Count;

                if (downServersCount == 0) {
                    logger.Debug("All servers are up");
                } else {
                    var dedicatedItemsOfDownServers = downServers.Select(server => server.DedicatedItem).ToList();
                    var dedicatedFileNamesOfDownServers = dedicatedItemsOfDownServers.Select(dedicatedItem => dedicatedItem.FileName);
                    logger.Warn($"Found {downServersCount} down {(downServersCount == 1 ? "server" : "servers")} ({String.Join(", ", dedicatedFileNamesOfDownServers)})");

                    logger.Info($"Launching down servers...");

                    List<LotrcServer> relaunchedServers;

                    try {
                        relaunchedServers = serverLauncher.LaunchSelectedServers(dedicatedItemsOfDownServers);
                        logger.Info($"Successfully re-launched servers");
                    } catch (Exception exc) {
                        logger.Error($"Failed to launch down servers: {exc.Message}");
                        relaunchedServers = new List<LotrcServer>();
                    }

                    serverList = serverObserver.ReplaceDownServers(serverList, relaunchedServers);
                }
            }

            #endregion
        }
    }
}
