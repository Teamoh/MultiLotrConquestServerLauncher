using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MultiLotrConquestServerLauncher {
    class MultiServerConfig {

        #region Properties

        private readonly string _filePath;
        private XmlDocument _xmlDoc;
        private Logger _logger = Logger.GetInstance();

        #endregion

        #region Getters & Setters

        public string TargetFileName {
            get {
                var targetFileName = _xmlDoc.SelectSingleNode("/Config/@TargetFileName").Value.Trim();

                if (!targetFileName.EndsWith(".xml")) {
                    targetFileName += ".xml";
                }

                return targetFileName;
            }
        }

        public string ServerFilePath {
            get {
                return _xmlDoc.SelectSingleNode("/Config/@ServerFilePath").Value.Trim();
            }
        }

        public string LogLevel {
            get {
                return _xmlDoc.SelectSingleNode("/Config/@LogLevel")?.Value;
            }
        }

        public List<DedicatedItem> DedicatedItems {
            get {
                var dedicatedItems = new List<DedicatedItem>();
                var dedicatedNodes = _xmlDoc.SelectNodes("/Config/Dedicated");

                foreach (XmlNode dedicatedNode in dedicatedNodes) {
                    var dedicatedFileName = dedicatedNode.Attributes["FileName"]?.Value;

                    if (string.IsNullOrEmpty(dedicatedFileName)) {
                        continue;
                    }

                    dedicatedFileName = dedicatedFileName.Trim();

                    if (!dedicatedFileName.EndsWith(".xml")) {
                        dedicatedFileName += ".xml";
                    }

                    int port;

                    try {
                        port = int.Parse(dedicatedNode.Attributes["Port"]?.Value);
                    } catch {
                        port = -1;
                    }

                    var dedicatedItem = new DedicatedItem {
                        FileName = dedicatedFileName,
                        GameName = dedicatedNode.Attributes["GameName"]?.Value,
                        Port = port,
                        RandomStartLevel = dedicatedNode.Attributes["RandomStartLevel"]?.Value == "true",
                        RandomLevelOrder = dedicatedNode.Attributes["RandomLevelOrder"]?.Value == "true"
                    };

                    dedicatedItems.Add(dedicatedItem);
                }

                return dedicatedItems;
            }
        }

        #endregion

        #region Constructor

        public MultiServerConfig(string filePath) {
            _filePath = filePath;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads the MultiServerConfig XML file
        /// </summary>
        public void Read() {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(_filePath);
            _xmlDoc = xmlDoc;
        }

        #endregion
    }
}
