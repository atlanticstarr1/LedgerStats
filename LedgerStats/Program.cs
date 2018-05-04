using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedgerStats
{
    public class Program
    {
        public class Tangle
        {
            private int _nodes; // Number of nodes in database
            private int _edges; // Number of edges
            private LinkedList<int>[] graph; // Using adjacency list to represent the graph.

            // Create a new DAG with N nodes
            public Tangle(int N)
            {
                if (N < 0) throw new Exception("Number of nodes must be positive.");
                _nodes = N;
                _edges = 0;
                graph = new LinkedList<int>[N];
                // create new list for each node.
                for (int n = 0; n < N; n++)
                {
                    graph[n] = new LinkedList<int>();
                }
            }

            // Return the number of nodes. We are starting at the first Node (ID=1), indstead ID=0;
            public int TotalNodes()
            {
                return _nodes - 1;
            }

            // Return the number of edges
            public int TotalEdges()
            {
                return _edges;
            }

            // Add edges to the DAG from n -> x and n-> y; add timestamp t at very end of list.
            public void AddEdge(int n, int x, int y, int t)
            {
                if (n < 0 || n >= _nodes) throw new Exception("Node " + n + " must be between 0 and " + TotalNodes());
                if (x < 0 || x >= _nodes) throw new Exception("Left parent " + x + " must be between 0 and " + TotalNodes());
                if (y < 0 || y >= _nodes) throw new Exception("Right parent " + y + " must be between 0 and " + TotalNodes());
                graph[n].AddFirst(x);
                _edges++;
                graph[n].AddLast(y);
                _edges++;
                graph[n].AddLast(t);
            }

            // Return node at postion n
            public IEnumerable<int> GetNode(int n)
            {
                if (n < 0 || n >= _nodes) throw new Exception();
                return graph[n];
            }

            // Print stats.
            public String PrintStats()
            {
                var Stats = new StringBuilder();
                var rate = GetRateOfIncomingTxns();
                Stats.Append(String.Format("\nAVG DAG DEPTH: {0:0.00}\n", GetAvgDagDepth()));
                Stats.Append(String.Format("AVG TXS PER DEPTH: {0:0.00}\n", GetAvgTxnPerDepth()));
                Stats.Append(String.Format("AVG REF: {0:0.000}\n", GetAvgRef()));
                Stats.Append(String.Format("AVG INCOMING TXS RATE: {0:0.00}\n", rate));    // λ
                Stats.Append(String.Format("TRANSACTION LATENCY: {0:0.0}\n", GetTransactionLatency()));    // Elapsed time before seeing transaction
                return Stats.ToString();
            }

            // AVG DAG DEPTH
            public double GetAvgDagDepth()
            {
                var depth = 0;  // store sum of min depths.
                for (int n = 0; n < _nodes; n++)
                {
                    depth += MinDepth(graph[n]);
                }
                return depth * 1.0 / TotalNodes();   // divide sum of min depths by total nodes.
            }

            // AVG TXS PER DEPTH
            public double GetAvgTxnPerDepth()
            {
                // Depth 0 (ID=1) is excluded. Assuming last txn is farthest from origin.
                var depth = MinDepth(graph[TotalNodes()]);
                return (TotalNodes() - 1 * 1.0) / depth;
            }

            // AVG REF
            public double GetAvgRef()
            {
                return (_edges * 1.0 / TotalNodes());
            }

            // TRANSACTION RATE
            public double GetRateOfIncomingTxns()
            {
                var totalTimeUnits = 0;
                double rate = 0.00;
                for (int n = 0; n < _nodes; n++)
                {
                    if (graph[n].Count > 0)
                        totalTimeUnits += graph[n].Last.Value;
                }

                if (totalTimeUnits != 0)
                    rate = (TotalNodes() - 1) * 1.0 / totalTimeUnits;
                return rate;
            }

            // TRANSACTION DELAY BETWEEN TRANSACTIONS
            public double GetTransactionLatency()
            {
                //calculate linear gradient
                var y2 = TotalNodes();
                var y1 = TotalNodes() / 2;
                var x2 = graph[y2].Last.Value;   // timestamp of last node
                var x1 = graph[y1].Last.Value;  // timestamp of middle node
                var gradient = (y2 - y1) / (x2 - x1);
                return gradient; // dy/dx
            }

            private int MinDepth(LinkedList<int> node)
            {
                var branchNode = node;  //save copy for right parent traversal
                var ldepth = 0;
                var rdepth = 0;
                // trunk node (left parent traversal)
                while (node.Count != 0)
                {
                    var neighbor1 = node.First.Value;
                    if (neighbor1 == 1)
                    {
                        ldepth++;
                        break;
                    }
                    else
                    {
                        ldepth++;
                        node = graph[neighbor1];
                    }
                }
                // branch node (right parent traversal)
                while (branchNode.Count != 0)
                {
                    var neighbor2 = branchNode.First.Next.Value;
                    if (neighbor2 == 1)
                    {
                        rdepth++;
                        break;
                    }
                    else
                    {
                        rdepth++;
                        branchNode = graph[neighbor2];
                    }
                }

                if (ldepth <= rdepth)
                    return ldepth;
                else
                    return rdepth;
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No file name specified");
                Console.ReadLine();
                return;
            }
            var N = 0;  // total nodes
            var line = 2; // start of node data
            string path = args[0];  // database text file
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                Console.WriteLine("File is empty.");
                Console.ReadLine();
                return;
            }
            N = int.Parse(lines[0]); // get total nodes
            var graph = new Tangle(N + 2); // create DAG

            foreach (string s in lines.Skip(1))
            {
                string[] numbers = s.Split(' ');
                var lparent = int.Parse(numbers[0]);
                var rparent = int.Parse(numbers[1]);
                var timestamp = int.Parse(numbers[2]);
                graph.AddEdge(line, lparent, rparent, timestamp);
                line++;
            }

            Console.WriteLine(graph.PrintStats());
            Console.ReadLine();
        }
    }

}
