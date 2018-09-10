using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Practik2_3
{
    public partial class Form1 : Form
    {
        
        public static int clusterNum;
        public static List<DataInstance> dataset;
        public static int[] clusterCount;
        public static DataInstance[] clusterCenter;
        public static int atrNum;
        public static double sumDist;
        public static string datainfo;
        public static int Rounds =0;
        DateTime dt1, dt2;
        double lAllTime = 0;
        double[] ParalelLocSum;
        public class DataInstance
        {
            public double[] attributes;
            public int cluster;
         
            public DataInstance(double[] input, int marker)
            {
                attributes = input;
                cluster = marker;
            }
        }

        public class MyDataComparer : IEqualityComparer<DataInstance>
        {
            public bool Equals(DataInstance x, DataInstance y)
            {
                for (int i = 0; i < x.attributes.Length; i++)
                {
                    if(x.attributes[i]!=y.attributes[i]){
                        return false;
                }
                }
                return true;
            }

            public int GetHashCode(DataInstance obj)
            {
                return 1;
            }
        }

        public double distEvkl(DataInstance pD1, DataInstance pD2) {
            double d = 0;
            for (int i = 0; i < pD1.attributes.Length; i++) {
                d =d+ Math.Pow(pD1.attributes[i] - pD2.attributes[i],2);
            }
            return Math.Sqrt(d);
        }
        public void updateMarkers()
        {
            foreach (DataInstance item in dataset)
            {
                double distToCur= item.cluster == 0 ? double.MaxValue : distEvkl(item, clusterCenter[item.cluster]);
                for (int j = 1; j <clusterNum; j++)
                {
                    double distToNew = distEvkl(item, clusterCenter[j]);
                    if (distToCur > distToNew)
                    {
                        clusterCount[item.cluster]--;
                        item.cluster = j;
                        clusterCount[j]++;
                        distToCur = distToNew;
                    }
                }
            }
        }


        public void ParallelUpdateMarkers()
        {
            var cts = new CancellationTokenSource();

            try
            {

                ParallelOptions Options = new ParallelOptions()
                {
                    CancellationToken = cts.Token,

                };
                var PortionData = Partitioner.Create(dataset);
                Parallel.ForEach(PortionData, Options, item =>
                {
                    double distToCur = item.cluster == 0 ? double.MaxValue : distEvkl(item, clusterCenter[item.cluster]);
                    for (int j = 1; j < clusterNum; j++)
                    {
                        double distToNew = distEvkl(item, clusterCenter[j]);
                        if (distToCur > distToNew)
                        {
                            Interlocked.Decrement(ref clusterCount[item.cluster]);                          
                            item.cluster = j;
                            Interlocked.Increment( ref clusterCount[j]);
                            distToCur = distToNew;
                        }
                    }
                });
            }
            catch (OperationCanceledException o)
            {
            }




        }
        public bool ParallelComputeCenters(double accuracy)
        {
            DataInstance[] lCenters = new DataInstance[clusterNum];
           
            ParalelLocSum = new double[clusterNum];
            Parallel.For(1, clusterNum, i =>
            {
                
                
                double[] centAtr = new double[atrNum];
                lCenters[i] = new DataInstance(centAtr, i + 1);
                foreach (var item in dataset.Where(x => x.cluster == i))
                {
                    for (int k = 0; k < item.attributes.Length; k++)
                    {
                        lCenters[i].attributes[k]+= item.attributes[k];
                    }
                }
                for (int k = 0; k < atrNum; k++)
                {
                    if (clusterCount[i] != 0)
                    {
                        lCenters[i].attributes[k] = lCenters[i].attributes[k] / clusterCount[i];
                    }
                }
                ParalelLocSum[i] += distEvkl(lCenters[i], clusterCenter[i]);
                
            });



            clusterCenter = lCenters;
            double locSum = ParalelLocSum.Sum();
            if (Math.Abs(locSum - sumDist) <= accuracy)
            {
                return true;
            }
            else
            {
                sumDist = locSum;
                return false;
            }
        }
        public bool computeCenters(double accuracy)
        {
            DataInstance[] lCenters = new DataInstance[clusterNum];
            for (int i = 1; i < clusterNum; i++)
            {
                double[] centAtr = new double[atrNum];
                lCenters[i] = new DataInstance(centAtr, i+1);
             
            }
            foreach (var item in dataset)
            {
                for (int i = 0; i < item.attributes.Length; i++)
                {
                    lCenters[item.cluster].attributes[i] += item.attributes[i];
                }
            }
            double locSum=0;
            for (int CurCent = 1; CurCent < clusterNum; CurCent++)
            {
                for (int i = 0; i < atrNum; i++)
                {
                    if (clusterCount[CurCent] != 0)
                    {
                        lCenters[CurCent].attributes[i] = lCenters[CurCent].attributes[i] / clusterCount[CurCent];
                    }
                }
                locSum += distEvkl(lCenters[CurCent] , clusterCenter[CurCent]);
            }
            clusterCenter = lCenters;
            if (Math.Abs(locSum - sumDist) <= accuracy)
            {
               

                return true;
            }
            else
            {
                sumDist = locSum;
                return false;
            }
        }
      

        public void ReadData(string path, int rowNumber)
        {
            dataset = new List<DataInstance>();
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
            {
                string line;
                line=sr.ReadLine();
                while (((line = sr.ReadLine()) != null) && rowNumber > 0)
                {
                    var strAtr = line.Split(' ');
                    atrNum = strAtr.Length;
                    double[] atr = new double[strAtr.Length];
                    /* for (int i = 0; i < atr.Length; i++)
                     {
                         double.TryParse(strAtr[i], out atr[i]);
                     }*/

                    double latitude, longtitude;
                    double.TryParse(strAtr[0], out latitude);
                    double.TryParse(strAtr[1], out longtitude);
                    double x, y;
                    //latitude
                    var latRad = latitude * Math.PI / 180;
                    var MercN = Math.Log(Math.Tan((Math.PI / 4) + (latRad / 2)));
                    atr[1] = 180 - (720 * MercN / (2 * Math.PI));

                    //get x from longtitude
                    atr[0] = (longtitude + 180) * 2;
                  
                    dataset.Add(new DataInstance(atr, 0));
                    rowNumber--;
                }
       
              
                if (rowNumber > 0)
                {
                    MessageBox.Show("Количество строк, указанное вами больше, чем размер файла. Считан весь файл", "Предупреждение", MessageBoxButtons.OK);
                }
            }
        }
     
        public Form1()
        {


            InitializeComponent();
        }

        private void downloadBTN_Click(object sender, EventArgs e)
        {
            ResTB.Clear();
            ResParallelTB.Clear();
      
            int rowNum;
            int.TryParse(countTB.Text, out rowNum);
            ReadData(@"E:\C#\data-mining\input2.txt", rowNum);
            panel1.Enabled = true;
            panel2.Enabled = true;
            backgroundWorker1.WorkerReportsProgress = true;
          
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker2.WorkerReportsProgress = true;

            backgroundWorker2.WorkerSupportsCancellation = true;
            datainfo = "загружено " + dataset.Count() + " сущностей размерности " + dataset[0].attributes.Length;
            drawData();
            drawParallelData();
            ResTB.AppendText(datainfo + Environment.NewLine);
            ResParallelTB.AppendText(datainfo + Environment.NewLine);
            startBTN.Enabled = true;
            startParallelBTN.Enabled = true;
            downloadBTN.Enabled = false;
        }

        public void drawData()
        {
            double picW = pictureBox1.Width-10;
            double picH = pictureBox1.Height-10;
   
            Graphics gr = pictureBox1.CreateGraphics();
            gr.Clear(Color.White);
            int maxX= 0;
            int maxY = 0;
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            foreach (var item in dataset)
            {
                if (minX > item.attributes[0])
                {
                    minX = (int)item.attributes[0];
                }
                if (maxX < item.attributes[0])
                {
                    maxX = (int)item.attributes[0];
                }
                if (maxY < item.attributes[1])
                {
                    maxY = (int)item.attributes[1];
                }
                if (minY > item.attributes[1])
                {
                    minY = (int)item.attributes[1];
                }
            }
      

            foreach (var item in dataset)
            {
                var ClusterCol = Color.FromArgb(item.cluster*40, 255 - item.cluster*40, item.cluster*40);
                 gr.DrawRectangle(new Pen(ClusterCol,3), 5+(int)( (item.attributes[0] -minX)* picW/ (maxX - minX)), 5+(int)( (item.attributes[1]-minY) * picH / (maxY - minY)), 5,5);
            }

            if (clusterCenter != null)
            {
                foreach (var item in clusterCenter)
                {
                    if (item != null)
                    {

                        gr.DrawRectangle(new Pen(Color.Magenta, 4), 5 + (int)((item.attributes[0] - minX) * picW / (maxX - minX)), 5 + (int)((item.attributes[1] - minY) * picH / (maxY - minY)), 4, 4);
                    }
                }
            }
        }
        public void drawParallelData()
        {
            double picW = pictureBox2.Width - 10;
            double picH = pictureBox2.Height - 10;

            Graphics gr = pictureBox2.CreateGraphics();
            gr.Clear(Color.White);
            int maxX = 0;
            int maxY = 0;
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            foreach (var item in dataset)
            {
                if (minX > item.attributes[0])
                {
                    minX = (int)item.attributes[0];
                }
                if (maxX < item.attributes[0])
                {
                    maxX = (int)item.attributes[0];
                }
                if (maxY < item.attributes[1])
                {
                    maxY = (int)item.attributes[1];
                }
                if (minY > item.attributes[1])
                {
                    minY = (int)item.attributes[1];
                }
            }


            foreach (var item in dataset)
            {
                var ClusterCol = Color.FromArgb(item.cluster * 40, 255 - item.cluster * 40, item.cluster * 40);
                gr.DrawRectangle(new Pen(ClusterCol, 3), 5 + (int)((item.attributes[0] - minX) * picW / (maxX - minX)), 5 + (int)((item.attributes[1] - minY) * picH / (maxY - minY)), 5, 5);
            }

            if (clusterCenter != null)
            {
                foreach (var item in clusterCenter)
                {
                    if (item != null)
                    {

                        gr.DrawRectangle(new Pen(Color.Magenta, 4), 5 + (int)((item.attributes[0] - minX) * picW / (maxX - minX)), 5 + (int)((item.attributes[1] - minY) * picH / (maxY - minY)), 4, 4);
                    }
                }
            }
        }
        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {

            if ((e.KeyChar <= 47 || e.KeyChar >= 58) && e.KeyChar != 8)
                e.Handled = true;

        }
        private void tb_TextChanged(object sender, EventArgs e)
        {
            if (countTB.Text.Length > 0)
            {
                downloadBTN.Enabled = true;
            }
            else
            {
                downloadBTN.Enabled = false;
            }
        }

        private void SingleWork(object sender, DoWorkEventArgs e)
        {
            lAllTime = 0;
            dt1 = DateTime.Now;
            double accuracy = 0;
            Double.TryParse(accuracyTB.Text,  out accuracy);
           clusterNum=5;
            clusterNum++;
            clusterCenter = new DataInstance[clusterNum];
            clusterCount = new int[clusterNum ];
            clusterCount[0] = dataset.Count;
          
            MyDataComparer cmp = new MyDataComparer();
            var DistData = dataset.Distinct(cmp).ToList();
            Random r = new Random();
            for (int i = 1; i < clusterNum; i++)
            {
                int rnd = r.Next(1, DistData.Count-1);
                clusterCenter[i] = DistData[rnd];
                DistData.RemoveAt(rnd);
            }
            
            clusterCenter[0] = null;
           sumDist = 10000;
            updateMarkers();
            while (!computeCenters(accuracy))
            {
                Rounds++;
                updateMarkers();
                backgroundWorker1.ReportProgress((int)((accuracy/sumDist)*100),clusterCount );
               
            }

            dt2 = DateTime.Now;
            lAllTime = dt2.Millisecond - dt1.Millisecond;
        }

        private void startBTN_Click(object sender, EventArgs e)
        {
            startBTN.Enabled = false;
            startParallelBTN.Enabled = false;            
            backgroundWorker1.RunWorkerAsync();
        }

        private void cancelBTN_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void startParallelBTN_Click(object sender, EventArgs e)
        {
            startBTN.Enabled = false;
            startParallelBTN.Enabled = false;
            backgroundWorker2.RunWorkerAsync();
        }

        private void cancelParallelBTN_Click(object sender, EventArgs e)
        {
            backgroundWorker2.CancelAsync();
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            lAllTime = 0;
            dt1 = DateTime.Now;
            double accuracy = 0;
            Double.TryParse(accuracyTB.Text, out accuracy);
            clusterNum = 5;
            clusterNum++;
            clusterCenter = new DataInstance[clusterNum];
            clusterCount = new int[clusterNum];
            clusterCount[0] = dataset.Count;

            MyDataComparer cmp = new MyDataComparer();
            var DistData = dataset.Distinct(cmp).ToList();
            Random r = new Random();
            for (int i = 1; i < clusterNum; i++)
            {
                int rnd = r.Next(1, DistData.Count - 1);
                clusterCenter[i] = DistData[rnd];
                DistData.RemoveAt(rnd);
            }

            clusterCenter[0] = null;
            sumDist = 10000;
            updateMarkers();
            while (!ParallelComputeCenters(accuracy))
            {
                Rounds++;
                ParallelUpdateMarkers();
                backgroundWorker2.ReportProgress((int)((accuracy / sumDist) * 100), clusterCount);

            }

            dt2 = DateTime.Now;
            lAllTime = dt2.Millisecond - dt1.Millisecond;
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ResParallelTB.AppendText("==================Окончательные результаты===========");
            for (int i = 0; i < clusterNum; i++)
            {
                ResParallelTB.AppendText(i + "  кластер " + clusterCount[i] + " элементов");
            }

            lAllTime = (dt2 - dt1).TotalMilliseconds;
            ResParallelTB.AppendText("Затрачено времени:" + lAllTime);
            ResParallelTB.AppendText("Итераций :" + Rounds);
            drawParallelData();            
            downloadBTN.Enabled = true;
        }

        private void SingleComplete(object sender, RunWorkerCompletedEventArgs e)
        {
           
            ResTB.AppendText("==================Окончательные результаты===========");
            for (int i = 0; i < clusterNum; i++)
            {
                ResTB.AppendText(i + "  кластер " + clusterCount[i] + " элементов");
            }

            lAllTime = (dt2 - dt1).TotalMilliseconds;
            ResTB.AppendText("Затрачено времени:" + lAllTime);
            ResTB.AppendText("Итераций :" + Rounds);
            drawData();
            downloadBTN.Enabled = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
         
           
       
           /* int[] uState = (int[])e.UserState;
            for (int i = 0; i < clusterNum; i++)
            {
                ResTB.AppendText(i + "  кластер " + uState[i] + " элементов");
            }*/
        }
    }
}
