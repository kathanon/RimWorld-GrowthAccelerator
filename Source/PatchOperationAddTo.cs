using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace GrowthAccelerator;
public class PatchOperationAddTo : PatchOperationPathed {
    public enum Order {
        Append,
        Prepend
    }

    public XmlContainer value;

    public Order order;

    protected override bool ApplyWorker(XmlDocument xml) {
        var valueNode = value.node;
        var list = valueNode.ChildNodes.Count == 1 ? valueNode.FirstChild : null;
        string name = list?.LocalName;
        bool result = false;
        foreach (object item in xml.SelectNodes(xpath)) {
            result = true;
            var xmlNode = item as XmlNode;
            var node = valueNode;
            if (name != null) {
                var match = xmlNode.ChildNodes.OfType<XmlNode>().Where(n => n.LocalName == name);
                if (match.Any()) {
                    xmlNode = match.First();
                    node = list;
                }
            }
            if (order == Order.Append) {
                foreach (XmlNode childNode in node.ChildNodes) {
                    xmlNode.AppendChild(xmlNode.OwnerDocument.ImportNode(childNode, deep: true));
                }
            } else if (order == Order.Prepend) {
                for (int num = node.ChildNodes.Count - 1; num >= 0; num--) {
                    xmlNode.PrependChild(xmlNode.OwnerDocument.ImportNode(node.ChildNodes[num], deep: true));
                }
            }
        }

        return result;
    }
}

