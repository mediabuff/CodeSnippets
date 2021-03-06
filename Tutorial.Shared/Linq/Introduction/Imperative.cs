﻿namespace Dixin.Linq.Introduction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml.XPath;

    internal static partial class Imperative
    {
        internal static void Sql()
        {
            using (DbConnection connection = new SqlConnection(
                @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\AdventureWorks_Data.mdf;Integrated Security=True;Connect Timeout=30"))
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT [Product].[Name]
                    FROM [Production].[Product] AS [Product]
                    LEFT OUTER JOIN [Production].[ProductSubcategory] AS [Subcategory] 
                        ON [Subcategory].[ProductSubcategoryID] = [Product].[ProductSubcategoryID]
                    LEFT OUTER JOIN [Production].[ProductCategory] AS [Category] 
                        ON [Category].[ProductCategoryID] = [Subcategory].[ProductCategoryID]
                    WHERE [Category].[Name] = @categoryName
                    ORDER BY [Product].[ListPrice] DESC";
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = "@categoryName";
                parameter.Value = "Bikes";
                command.Parameters.Add(parameter);
                connection.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string productName = (string)reader["Name"];
                        Trace.WriteLine(productName);
                    }
                }
            }
        }
    }

    internal static partial class Imperative
    {
        internal static void Xml()
        {
            XPathDocument feed = new XPathDocument("https://weblogs.asp.net/dixin/rss");
            XPathNavigator navigator = feed.CreateNavigator();
            XPathExpression selectExpression = navigator.Compile("//item[guid/@isPermaLink='true']/title/text()");
            XPathExpression sortExpression = navigator.Compile("../../pubDate/text()");
            selectExpression.AddSort(sortExpression, new DateTimeComparer());
            XPathNodeIterator nodes = navigator.Select(selectExpression);
            foreach (object node in nodes)
            {
                Trace.WriteLine(node);
            }
        }
    }

    public class DateTimeComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return Convert.ToDateTime(x).CompareTo(Convert.ToDateTime(y));
        }
    }

    internal static partial class Imperative
    {
        internal static void DelegateTypes()
        {
            Assembly coreLibrary = typeof(object).GetTypeInfo().Assembly;
            Dictionary<string, List<Type>> delegateTypes = new Dictionary<string, List<Type>>();
            foreach (Type type in coreLibrary.GetExportedTypes())
            {
                if (type.GetTypeInfo().BaseType == typeof(MulticastDelegate))
                {
                    if (!delegateTypes.TryGetValue(type.Namespace, out List<Type> namespaceTypes))
                    {
                        namespaceTypes = delegateTypes[type.Namespace] = new List<Type>();
                    }
                    namespaceTypes.Add(type);
                }
            }
            List<KeyValuePair<string, List<Type>>> delegateTypesList =
                new List<KeyValuePair<string, List<Type>>>(delegateTypes);
            for (int index = 0; index < delegateTypesList.Count - 1; index++)
            {
                int currentIndex = index;
                KeyValuePair<string, List<Type>> after = delegateTypesList[index + 1];
                while (currentIndex >= 0)
                {
                    KeyValuePair<string, List<Type>> before = delegateTypesList[currentIndex];
                    int compare = before.Value.Count.CompareTo(after.Value.Count);
                    if (compare == 0)
                    {
                        compare = after.Key.CompareTo(before.Key);
                    }
                    if (compare >= 0)
                    {
                        break;
                    }
                    delegateTypesList[currentIndex + 1] = delegateTypesList[currentIndex];
                    currentIndex--;
                }
                delegateTypesList[currentIndex + 1] = after;
            }
            foreach (KeyValuePair<string, List<Type>> namespaceTypes in delegateTypesList) // Output.
            {
                Trace.Write(namespaceTypes.Value.Count + " " + namespaceTypes.Key + ":");
                foreach (Type delegateType in namespaceTypes.Value)
                {
                    Trace.Write(" " + delegateType.Name);
                }
                Trace.WriteLine(null);
            }
            // 30 System: Action`1 Action Action`2 Action`3 Action`4 Func`1 Func`2 Func`3 Func`4 Func`5 Action`5 Action`6 Action`7 Action`8 Func`6 Func`7 Func`8 Func`9 Comparison`1 Converter`2 Predicate`1 ResolveEventHandler AssemblyLoadEventHandler AppDomainInitializer CrossAppDomainDelegate AsyncCallback ConsoleCancelEventHandler EventHandler EventHandler`1 UnhandledExceptionEventHandler
            // 8 System.Threading: SendOrPostCallback ContextCallback ParameterizedThreadStart WaitCallback WaitOrTimerCallback IOCompletionCallback ThreadStart TimerCallback
            // 3 System.Reflection: ModuleResolveEventHandler MemberFilter TypeFilter
            // 3 System.Runtime.CompilerServices: TryCode CleanupCode CreateValueCallback
            // 2 System.Runtime.Remoting.Messaging: MessageSurrogateFilter HeaderHandler
            // 1 System.Runtime.InteropServices: ObjectCreationDelegate
            // 1 System.Runtime.Remoting.Contexts: CrossContextDelegate
        }
    }

    internal class WebContentDownloader
    {
        internal string Download(string uri)
        {
            throw new NotImplementedException();
        }
    }

    internal class WordConverter
    {
        internal FileInfo Convert(string html)
        {
            throw new NotImplementedException();
        }
    }

    internal class OneDriveUploader
    {
        internal void Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }

    internal class DocumentBuilder
    {
        private readonly WebContentDownloader downloader;
        private readonly WordConverter converter;
        private readonly OneDriveUploader uploader;

        internal DocumentBuilder(WebContentDownloader downloader, WordConverter converter, OneDriveUploader uploader)
        {
            this.downloader = downloader;
            this.converter = converter;
            this.uploader = uploader;
        }

        internal void Build(string uri)
        {
            string html = this.downloader.Download(uri);
            FileInfo word = this.converter.Convert(html);
            this.uploader.Upload(word);
        }
    }

    internal partial class Imperative
    {
        internal static void BuildDocument()
        {
            DocumentBuilder builder = new DocumentBuilder(
                new WebContentDownloader(), new WordConverter(), new OneDriveUploader());
            builder.Build("https://weblogs.asp.net/dixin/linq-via-csharp");
        }
    }
}
