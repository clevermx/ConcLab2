using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practik2_2
{
    class Program
    {

       static string[] files;
        static char[] separators= { ' ', ',', '-', '.', '!', '?', '\"', '\n', '\r' };
        static Dictionary<string, int> mWords;
        static Dictionary<int, int> mLengthStat;

        static ConcurrentDictionary<string, int> mConcWords;
        static ConcurrentDictionary<int, int> mConcLengthStat;
        static string[] TopFreq;
        static void InitFiles()
        {
            files = Directory.EnumerateFiles("E:\\C#", "*.*", SearchOption.AllDirectories).ToArray();

            

            Console.WriteLine("Число файлов: " + files.Length );
        }
        static string[] fMasOfNames(string fMainName) {
            return Directory.EnumerateFiles(fMainName, "*.*", SearchOption.AllDirectories).ToArray();
        }
        static void CalcFreqStat()
        {
            mWords = new Dictionary<string, int>();
            string[] lWords;
            for (int i = 0; i < files.GetLength(0); i++)
            {

                using (StreamReader sr = new StreamReader(files[i], Encoding.Default))
                {
                    string line;
                    while (sr.EndOfStream!=true)
                    {
                        line = sr.ReadLine();
                        lWords = line.Split(separators);

                        foreach (var word in lWords)
                        {
                            if (mWords.ContainsKey(word.ToLower()))
                            {
                                mWords[word.ToLower()]++;
                                
                            }
                            else
                            {
                                mWords.Add(word.ToLower(), 1);
                            }
                        }
                    }
                }
            }
        }
        static void CalcLengthDisp()
        {
          mLengthStat= new Dictionary<int, int>();
            string[] lWords;
            for (int i = 0; i < files.GetLength(0); i++)
            {

                using (StreamReader sr = new StreamReader(files[i], Encoding.Default))
                {
                    string line;
                    while (sr.EndOfStream != true)
                    {
                        line = sr.ReadLine();
                        lWords = line.Split(separators);

                        foreach (var word in lWords)
                        {
                            if (mLengthStat.ContainsKey(word.Length))
                            {
                                mLengthStat[word.Length]++;
                            }
                            else
                            {
                                mLengthStat.Add(word.Length, 1);
                            }


                        }
                    }
                }
            }
        }

        static void CalcTopFreq()
        {
            TopFreq = new String[10];
            string lMaxStr="";
            int lMaxFreq;
            for (int i = 0; i < 10; i++)
            {
                lMaxFreq = int.MinValue;
                foreach (var W_F in mWords)
                {
                    if (W_F.Value > lMaxFreq)
                    {
                        lMaxFreq = W_F.Value;
                        lMaxStr = W_F.Key;
                    }
                }
                TopFreq[i] = lMaxStr;
                mWords.Remove(lMaxStr);
            }
        }
        static void LinqFreqStat()
        {
            mWords = files.Select(f => File.ReadAllText(f, Encoding.Default)).SelectMany(s => s.Split(separators)).GroupBy(s => s.ToLower()).ToDictionary(slovo => slovo.Key, slovo => slovo.Count());
        }

        static void LinqLenDisp()
        {
             mLengthStat= files.Select(f => File.ReadAllText(f, Encoding.Default)).SelectMany(s => s.Split(separators)).GroupBy(s =>s.Length).ToDictionary(group=>group.Key, group=>group.Count());
        }
        static void LinqFreqTop()
        {
           TopFreq  = mWords.OrderByDescending(g => g.Value).Take(10).Select(g=>g.Key).ToArray<string>();
        }



        static void PLinqFreqStat()
        {
            mWords =files.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered).Select(f => File.ReadAllText(f, Encoding.Default)).SelectMany(s => s.Split(separators)).GroupBy(s => s.ToLower()).ToDictionary(slovo => slovo.Key, slovo => slovo.Count());
        }

        static void PLinqLenDisp()
        {
            mLengthStat = files.AsParallel().WithMergeOptions(ParallelMergeOptions.AutoBuffered).Select(f => File.ReadAllText(f, Encoding.Default)).SelectMany(s => s.Split(separators)).GroupBy(s => s.Length).ToDictionary(group => group.Key, group => group.Count());
        }
        static void PLinqFreqTop()
        {
            TopFreq = mWords.AsParallel().WithMergeOptions(ParallelMergeOptions.AutoBuffered).OrderByDescending(g => g.Value).Take(10).Select(g => g.Key).ToArray<string>();
        }


        static void ParalelFreqStat()
        {
            mConcWords= new ConcurrentDictionary<string, int>();
            string[] lWords;
            Parallel.ForEach(files, f =>
            {
                using (StreamReader sr = new StreamReader(f, Encoding.Default))
                {
                    string line;
                    while (sr.EndOfStream != true)
                    {
                        line = sr.ReadLine();
                        lWords = line.Split(separators);

                        foreach (var word in lWords)
                        {
                            mConcWords.AddOrUpdate(word,1, (k,v)=>v+1);
                        }
                    }
                }
            });
        }


        static void ParalelLengthDisp()
        {
            mConcLengthStat = new ConcurrentDictionary<int, int>();
            string[] lWords;
            Parallel.ForEach(files, f =>
            {

                using (StreamReader sr = new StreamReader(f, Encoding.Default))
                {
                    string line;
                    while (sr.EndOfStream != true)
                    {
                        line = sr.ReadLine();
                        lWords = line.Split(separators);

                        foreach (var word in lWords)
                        {
                            mConcWords.AddOrUpdate(word, 1, (k, v) => v + 1);
                        }
                    }
                }
            });
        }

        static void ParalelTopFreq()
        {
            TopFreq = new String[10];
            string lMaxStr = "";
            int lMaxFreq;
            for (int i = 0; i < 10; i++)
            {
                lMaxFreq = int.MinValue;
                foreach (var W_F in mConcWords)
                {
                    if (W_F.Value > lMaxFreq)
                    {
                        lMaxFreq = W_F.Value;
                        lMaxStr = W_F.Key;
                    }
                }
                TopFreq[i] = lMaxStr;
                mConcWords.TryRemove(lMaxStr, out lMaxFreq);
            }
        }
        static void Main(string[] args)
        {
            int povtor = 10;
            Console.WriteLine("**************************************");
            Console.WriteLine("==========последовательно=============");
            InitFiles();
            System.Diagnostics.Stopwatch MyStopWatch = new System.Diagnostics.Stopwatch();
            MyStopWatch.Start();
            for (int i = 0; i < povtor; i++)
            {
                //CalcFreqStat();
                //LinqFreqStat();
                // ParalelFreqStat();
                PLinqFreqStat();
            }
            MyStopWatch.Stop();
            Console.WriteLine("частота слов : " +MyStopWatch.ElapsedMilliseconds/povtor + " мс");
            MyStopWatch = new System.Diagnostics.Stopwatch();
            MyStopWatch.Start();
            for (int i = 0; i < povtor; i++)
            {
                // CalcLengthDisp();
                 //LinqLenDisp();
                // ParalelLengthDisp();
               PLinqLenDisp();
            }
            MyStopWatch.Stop();
            Console.WriteLine("длина слов : " + MyStopWatch.ElapsedMilliseconds / povtor + " мс");
            MyStopWatch = new System.Diagnostics.Stopwatch();
            MyStopWatch.Start();

            //CalcTopFreq();
           //  LinqFreqTop();
            //ParalelTopFreq();
             PLinqFreqTop();

            MyStopWatch.Stop();
            Console.WriteLine("топ 10 по частоте " + MyStopWatch.ElapsedMilliseconds + " мс");
            foreach (var word in TopFreq) {
                Console.WriteLine(word);
            }
            Console.ReadLine();
        }
    }
}
