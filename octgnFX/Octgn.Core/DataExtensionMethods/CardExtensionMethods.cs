﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Diagnostics;
using Octgn.Library;
using Octgn.Library.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Octgn.Core.DataManagers;
using Octgn.DataNew.Entities;

using log4net;
using Octgn.Library.Localization;

namespace Octgn.Core.DataExtensionMethods
{

    public static class CardExtensionMethods
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly ReaderWriterLockSlim GetSetLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly Dictionary<Guid, Set> CardSetIndex = new Dictionary<Guid, Set>();

        public static Set GetSet(this ICard card)
        {
            try
            {
                GetSetLock.EnterUpgradeableReadLock();
                Set ret = null;
                if (!CardSetIndex.TryGetValue(card.SetId, out ret))
                {
                    GetSetLock.EnterWriteLock();
                    ret = SetManager.Get().GetById(card.SetId);
                    CardSetIndex[card.SetId] = ret;
                    GetSetLock.ExitWriteLock();
                }
                return ret;
            }
            finally
            {
                GetSetLock.ExitUpgradeableReadLock();
            }
        }
        public static MultiCard ToMultiCard(this ICard card, int quantity = 1, bool clone = true)
        {
            if (clone)
            {
                var ret = new MultiCard();
                ret.Alternate = card.Alternate.Clone() as String;
                ret.Id = card.Id;
                ret.ImageUri = card.ImageUri.Clone() as String;
                ret.Name = card.Name.Clone() as String;
                ret.Quantity = quantity;
                ret.SetId = card.SetId;
                ret.Properties = card.Properties.ToDictionary(x => x.Key, y => y.Value);
                ret.Size = new CardSize()
                {
					Name = card.Size.Name.Clone() as String,
                    Back = card.Size.Back.Clone() as String,
                    Front = card.Size.Front.Clone() as String,
                    Width = card.Size.Width,
                    Height = card.Size.Height
                };
                return ret;
            }
            else
            {
                var ret = new MultiCard();
                ret.Alternate = card.Alternate;
                ret.Id = card.Id;
                ret.ImageUri = card.ImageUri;
                ret.Name = card.Name;
                ret.Quantity = quantity;
                ret.SetId = card.SetId;
                ret.Properties = card.Properties;
                ret.Size = card.Size;
                return ret;
            }
        }
        public static string GetPicture(this ICard card)
        {
            if (String.IsNullOrWhiteSpace(card.ImageUri) == false && card.ImageUri.StartsWith("pack://", StringComparison.InvariantCultureIgnoreCase))
            {
                return card.ImageUri;
            }
            var set = card.GetSet();

            Uri uri = null;

            var path = set.ImagePackUri;
            if (Directory.Exists(path) == false)
            {
                throw new UserMessageException(L.D.Exception__CanNotFindDirectoryGameDefBroken_Format,path);
            }
            var files = Directory.GetFiles(set.ImagePackUri, card.GetImageUri() + ".*").OrderBy(x => x.Length).ToArray();
            if (files.Length == 0) //Generate or grab proxy
            {
                files = Directory.GetFiles(set.ProxyPackUri, card.GetImageUri() + ".png");
                if (files.Length != 0)
                {
                    uri = new Uri(files.First());
                }
            }
            else
                uri = new Uri(files.First());

            if (uri == null)
            {
                uri = new System.Uri(Path.Combine(set.ProxyPackUri, card.GetImageUri() + ".png"));
                if (X.Instance.ReleaseTest || X.Instance.Debug)
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    set.GetGame().GetCardProxyDef().SaveProxyImage(card.GetProxyMappings(), uri.LocalPath);
                    s.Stop();
                    if (s.ElapsedMilliseconds > 200)
                    {
                        //System.Diagnostics.Trace.TraceEvent(TraceEventType.Warning, 1|0, "Proxy gen lagged by " + s.ElapsedMilliseconds - 200 + " ms");
                        Log.WarnFormat("Proxy gen lagged by {0} ms", s.ElapsedMilliseconds - 200);
                    }
                    else Log.InfoFormat("Proxy gen took {0} ms", s.ElapsedMilliseconds);
                }
                else
                {
                    set.GetGame().GetCardProxyDef().SaveProxyImage(card.GetProxyMappings(), uri.LocalPath);
                }
                return uri.LocalPath;
            }
            else
            {
                return uri.LocalPath;
            }
        }

        public static string GetProxyPicture(this ICard card)
        {
            var set = card.GetSet();

            Uri uri = new System.Uri(Path.Combine(set.ProxyPackUri, card.GetImageUri() + ".png"));

            var files = Directory.GetFiles(set.ProxyPackUri, card.GetImageUri() + ".png");

            if (files.Length == 0)
            {
                set.GetGame().GetCardProxyDef().SaveProxyImage(card.GetProxyMappings(), uri.LocalPath);
            }

            return uri.LocalPath;
        }

        public static string GetImageUri(this ICard card)
        {
            var ret = card.ImageUri;
            if (!String.IsNullOrWhiteSpace(card.Alternate)) ret = ret + "." + card.Alternate;
            return ret;
        }

        public static bool HasProperty(this Card card, string name)
        {
            return card.Properties[card.Alternate].Properties.Any(x => x.Key.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && x.Key.IsUndefined == false);
        }

        public static Dictionary<string, string> GetProxyMappings(this ICard card)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (KeyValuePair<PropertyDef, object> kvi in card.PropertySet())
            {
                ret.Add(kvi.Key.Name, kvi.Value.ToString());
            }
            return (ret);
        }

        public static IDictionary<PropertyDef, object> PropertySet(this ICard card)
        {
            var ret = card.Properties[card.Alternate].Properties.Where(x => x.Key.IsUndefined == false).ToDictionary(x => x.Key, x => x.Value);
            return ret;
        }

        public static void SetPropertySet(this Card card, string propertyType = "")
        {
            if (String.IsNullOrWhiteSpace(propertyType)) propertyType = "";
            if (card.Properties.Any(x => x.Key.Equals(propertyType, StringComparison.InvariantCultureIgnoreCase)))
                card.Alternate = propertyType;
        }

        public static string PropertyName(this Card card)
        {
            return
                card.PropertySet()
                    .First(x => x.Key.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase))
                    .Value as string;
        }

        public static MultiCard Clone(this MultiCard card)
        {
            var ret = new MultiCard
                          {
                              Name = card.Name.Clone() as string,
                              Id = card.Id,
                              Alternate = card.Alternate.Clone() as string,
                              ImageUri = card.ImageUri.Clone() as string,
                              Quantity = card.Quantity,
                              Properties = new Dictionary<string, CardPropertySet>(),
                              SetId = card.SetId
                          };
            foreach (var p in card.Properties)
            {
                ret.Properties.Add(p.Key, p.Value);
            }
            return ret;
        }

        public static Card Clone(this Card card)
        {
            if (card == null) return null;
            var ret = new Card
                          {
                              Name = card.Name.Clone() as string,
                              Alternate = card.Alternate.Clone() as string,
                              Id = card.Id,
                              ImageUri = card.ImageUri.Clone() as string,
                              Properties =
                                  card.Properties.ToDictionary(
                                      x => x.Key.Clone() as string, x => x.Value.Clone() as CardPropertySet),
                              SetId = card.SetId,
                              Size = new CardSize()
                              {
                                  Back = card.Size.Back.Clone() as string,
								  Front = card.Size.Front.Clone() as string,
								  Height = card.Size.Height,
								  Width = card.Size.Width,
								  Name = card.Size.Name.Clone() as string
                              }
                          };
            return ret;
        }
    }
}