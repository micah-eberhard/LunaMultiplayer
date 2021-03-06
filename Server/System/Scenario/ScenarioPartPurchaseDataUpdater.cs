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
        /// We received a part purchase message so update the scenario file accordingly
        /// </summary>
        public static void WritePartPurchaseDataToFile(ShareProgressPartPurchaseMsgData partPurchaseMsg)
        {
            Task.Run(() =>
            {
                lock (Semaphore.GetOrAdd("ResearchAndDevelopment", new object()))
                {
                    if (!ScenarioStoreSystem.CurrentScenariosInXmlFormat.TryGetValue("ResearchAndDevelopment", out var xmlData)) return;

                    var updatedText = UpdateScenarioWithPartPurchaseData(xmlData, partPurchaseMsg.TechId, partPurchaseMsg.PartName);
                    ScenarioStoreSystem.CurrentScenariosInXmlFormat.TryUpdate("ResearchAndDevelopment", updatedText, xmlData);
                }
            });
        }

        /// <summary>
        /// Patches the scenario file with part purchase data
        /// </summary>
        private static string UpdateScenarioWithPartPurchaseData(string scenarioData, string techId, string partName)
        {
            var document = new XmlDocument();
            document.LoadXml(scenarioData);

            var techNode = document.SelectSingleNode($"/{ConfigNodeXmlParser.StartElement}/{ConfigNodeXmlParser.ParentNode}[@name='Tech']" +
                                                    $@"/{ConfigNodeXmlParser.ValueNode}[@name='id' and text()=""{techId}""]" +
                                                    $"/parent::{ConfigNodeXmlParser.ParentNode}[@name='Tech']");
            if (techNode != null)
            {
                var newPart = ConfigNodeXmlParser.CreateXmlParameter("part", document);
                newPart.InnerXml = partName;

                techNode.AppendChild(newPart);
            }

            return document.ToIndentedString();
        }
    }
}
