using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace WebCrawler
{
    class WebCrawler
    {
        static void Main(string[] args)
        {

            String indexPath = @"C:\Users\Brandon\Desktop\Multimedia Retrieval\W3 Files\Index";
            //Analyzers build token streams which analyze text
            Analyzer analyzer = new StandardAnalyzer();
            IndexWriter writer = new IndexWriter(indexPath, analyzer, true);
            
            //Set the seedUrl and initialize the crawler
            String seedUrl = "http://sydney.edu.au/engineering/it/";
            WebCrawler crawler = new WebCrawler();
            Queue<String> linkQueue = new Queue<String>();
            linkQueue.Enqueue(seedUrl);
            HashSet<String> linkSet = new HashSet<String>();
            Console.Write("Sites Explored: 0");

            //Iteratively extract links from the first URL in the frontier 
            //and adds its content to index
            while (linkQueue.Count != 0 && linkSet.Count < 50)
            {
                String currentLink = linkQueue.Dequeue();
                try
                {
                    if (linkSet.Contains(currentLink)) continue;
                    String content = crawler.getUrlContent(currentLink);
                    crawler.getLinks(linkQueue, content, currentLink);
                    linkSet.Add(currentLink);
                    Document doc = new Document();
                    doc.Add(new Field("link", currentLink, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED));
                    writer.AddDocument(doc);

                    Console.Write("\rSites Explored: {0}", linkSet.Count);
                }
                catch (Exception) { continue; }
            }
            writer.Optimize();
            writer.Close();
            Console.WriteLine();

            //Execute the search
            String search = "suits";
            QueryParser parser = new QueryParser("content", analyzer);
            Query query = parser.Parse(search);
            var searcher = new IndexSearcher(indexPath);
            Hits hits = searcher.Search(query);
            int results = hits.Length();
            Console.WriteLine("Found {0} results for \"{1}\"", results, search);
            for (int i = 0; i < results; i++)
            {
                Document doc = hits.Doc(i);
                float score = hits.Score(i);
                Console.WriteLine("Result num {0}, score {1}", i + 1, score);
                Console.WriteLine("URL: {0}", doc.Get("link"));
            }
        }

        //Returns the HTML source code of 'url'
        public String getUrlContent(String url)
        {
            StringBuilder sb = new StringBuilder();

            // buffer
            byte[] buf = new byte[8192];

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                // read data into buffer
                count = resStream.Read(buf, 0, buf.Length);

                // if we read any data
                if (count != 0)
                {
                    // put into a temporary String
                    tempString = Encoding.UTF8.GetString(buf, 0, count);

                    // append to the StringBuilder
                    sb.Append(tempString);
                }
            }
            while (count > 0); 

            return sb.ToString();
        }

        public void getLinks(Queue<String> linkQueue, String html, String url)
        {
            var baseUrl = new Uri(url);

             HtmlDocument doc = new HtmlDocument();
             doc.LoadHtml(html);

             foreach(HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]") )
             {
                 String linkUrl = link.Attributes["href"].Value;

                 // Convert relative path to absolute url
                 Uri absoluteUrl = new Uri(baseUrl, linkUrl);

                 //put the anchor link into the global hashset
                 linkQueue.Enqueue(absoluteUrl.AbsoluteUri.ToString());
             }
        }
    }

    
}