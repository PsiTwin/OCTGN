﻿using Octgn.ProxyGenerator.Definitions;
using Octgn.ProxyGenerator.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Octgn.ProxyGenerator
{
    public class TemplateManager
    {
        private List<CardDefinition> templates = null;

        public string DefaultID { get; set; }
        public bool UseMultiFieldMatching { get; set; }

        private List<TemplateMapping> templateMappings;
        private List<string> matchMapping;

        public TemplateManager()
        {
            templates = new List<CardDefinition>();
            templateMappings = new List<TemplateMapping>();
            matchMapping = new List<string>();
        }

        public void AddTemplate(CardDefinition cardDef)
        {
            if (templates.Contains(cardDef))
            {
                templates.Remove(cardDef);
            }
            templates.Add(cardDef);
        }

        public CardDefinition GetTemplate(Dictionary<string,string> values)
        {
            CardDefinition ret = GetDefaultTemplate();
            if (UseMultiFieldMatching)
            {
                CardDefinition temp = MultiMatcher.GetTemplate(templates, values, matchMapping);
                if (temp != null)
                {
                    ret = temp;
                }
            }
            else
            {
                string field = templateMappings[0].Name;
                if (field != null && values.ContainsKey(field))
                {
                    ret = GetTemplate(values[field]);
                }
            }
            
            return (ret);
        }

        public CardDefinition GetTemplate(string ID)
        {
            CardDefinition ret = GetDefaultTemplate();
            foreach (CardDefinition cardDef in templates)
            {
                if (cardDef.id == ID)
                {
                    ret = cardDef;
                    break;
                }
            }
            return (ret);
        }

        public CardDefinition GetDefaultTemplate()
        {
            foreach (CardDefinition cardDef in templates)
            {
                if (cardDef.id == DefaultID)
                {
                    return cardDef;
                }
            }
            return null;
        }

        public void ClearTemplates()
        {
            templates.Clear();
        }

        public void AddMapping(string field)
        {
            AddMapping(field, string.Empty);
        }

        public void AddMapping(string field, string mapTo)
        {
            TemplateMapping mapping = new TemplateMapping()
            {
                Name = field,
                MapTo = mapTo
            };
            if (ContainsMapping(mapping))
            {
                templateMappings.Remove(mapping);
            }
            templateMappings.Add(mapping);
        }

        public bool ContainsMapping(string field)
        {
            TemplateMapping mapping = new TemplateMapping()
            {
                Name = field,
                MapTo = string.Empty
            };
            return (ContainsMapping(mapping));
        }

        public bool ContainsMapping(TemplateMapping mapping)
        {
            foreach (TemplateMapping map in templateMappings)
            {
                if (map.Equals(mapping))
                {
                    return (true);
                }
            }
            return (false);
        }

        public void AddMatch(string value)
        {
            if(ContainsMatch(value))
            {
                matchMapping.Remove(value);
            }
            matchMapping.Add(value);
        }

        public bool ContainsMatch(string value)
        {
            return (matchMapping.Contains(value));
        }
    }
}
