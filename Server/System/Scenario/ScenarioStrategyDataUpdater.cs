﻿using LunaCommon.Message.Data.ShareProgress;
using LunaCommon.Xml;
using Server.Utilities;
using System.Threading.Tasks;
using System.Xml;

namespace Server.System.Scenario
{
    public partial class ScenarioDataUpdater
    {
        /// <summary>
        /// We received a strategy message so update the scenario file accordingly
        /// </summary>
        public static void WriteStrategyDataToFile(StrategyInfo strategy)
        {
            Task.Run(() =>
            {
                lock (Semaphore.GetOrAdd("StrategySystem", new object()))
                {
                    if (!ScenarioStoreSystem.CurrentScenariosInXmlFormat.TryGetValue("StrategySystem", out var xmlData)) return;

                    var updatedText = UpdateScenarioWithStrategyData(xmlData, strategy);
                    ScenarioStoreSystem.CurrentScenariosInXmlFormat.TryUpdate("StrategySystem", updatedText, xmlData);
                }
            });
        }

        /// <summary>
        /// Patches the scenario file with strategy data
        /// </summary>
        private static string UpdateScenarioWithStrategyData(string scenarioData, StrategyInfo strategy)
        {
            var document = new XmlDocument();
            document.LoadXml(scenarioData);

            var strategiesList = document.SelectSingleNode($"/{ConfigNodeXmlParser.StartElement}/{ConfigNodeXmlParser.ParentNode}[@name='STRATEGIES']");
            if (strategiesList != null)
            {
                var receivedStrategy = DeserializeAndImportNode(strategy.Data, strategy.NumBytes, document);
                if (receivedStrategy != null)
                {
                    var existingStrategy = strategiesList.SelectSingleNode($"{ConfigNodeXmlParser.ParentNode}[@name='STRATEGY']/" +
                                                                           $@"{ConfigNodeXmlParser.ValueNode}[@name='name' and text()=""{strategy.Name}""]/" +
                                                                           $"parent::{ConfigNodeXmlParser.ParentNode}[@name='STRATEGY']");
                    if (existingStrategy != null)
                    {
                        //Replace the existing stragegy value with the received one
                        existingStrategy.InnerXml = receivedStrategy.InnerXml;
                    }
                    else
                    {
                        var newStrategyNode = ConfigNodeXmlParser.CreateXmlNode("STRATEGY", document);
                        newStrategyNode.InnerXml = receivedStrategy.InnerXml;
                        strategiesList.AppendChild(newStrategyNode);
                    }
                }
            }

            return document.ToIndentedString();
        }
    }
}
