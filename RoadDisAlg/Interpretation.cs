using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoadDisAlg
{
    /// <summary>
    /// 加密线段上的点
    /// </summary>
    public class Interpretation
    {
        /// <summary>
        /// 道路网
        /// </summary>
        private RoadNetWork _netWork=null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="netWork">道路网</param>
        public Interpretation(RoadNetWork netWork)
        {
            this._netWork = netWork;
        }

        /// <summary>
        /// 加密
        /// </summary>
        public void Interpretate()
        {
            float w = ComAveLineLength();       //计算平均线段长度
            int count = _netWork.PointList.Count;
            foreach (Road curR in _netWork.RoadList)
            {
                int pCount = curR.PointList.Count;
                int RID = curR.RID;
                int i = 0;                //当前处理的线段的索引号
                int curIndex = 0;         //当前插入第一个新顶点的位置
                List<PointCoord> resPList=null;
                while (i < pCount - 1)
                {
                    PointCoord p1 = _netWork.PointList[curR.PointList[curIndex]];
                    PointCoord p2 = _netWork.PointList[curR.PointList[curIndex + 1]];
                    resPList = InterpretateLine(p1, p2, w, RID);
                    if (resPList != null)
                    {
                        for (int j = 0; j < resPList.Count; j++)
                        {
                            PointCoord p = resPList[j];
                            p.ID = count;
                            _netWork.PointList.Add(p);
                            count++;
                            curR.PointList.Insert(curIndex + 1 + j, p.ID);
                        }
                        curIndex = curIndex + resPList.Count + 1;
                        i++;
                    }
                    else
                    {
                        curIndex = curIndex+ 1;
                        i++;
                    } 
                }
            }
        }

        /// <summary>
        /// 计算线段平均长度
        /// </summary>
        /// <returns>平均长度</returns>
        private float ComAveLineLength()
        {
            int count=0;
            double sum=0;
            foreach (Road curR in _netWork.RoadList)
            {
                int pCount = curR.PointList.Count;
                for (int i = 0; i < pCount-1; i++)
                {
                    PointCoord p1 = _netWork.PointList[curR.PointList[i]];
                    PointCoord p2 = _netWork.PointList[curR.PointList[i + 1]];
                    double length = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                    sum = sum + length;
                    count++;
                }
            }
            if (count == 0)
                return -1f;
            return (float)(sum / count);
        }

        /// <summary>
        /// 向一段线段中添加加密点
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="w">平均线段长度</param>
        /// <returns></returns>
        private List<PointCoord> InterpretateLine(PointCoord p1, PointCoord p2,float w,int RID)
        {
           /* if (p1.ID == 107 && p2.ID == 108)
            {
                int error = 0;
            }*/
            
            float length = (float )Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            if (w>=length)
                return null;
            int k = (int)Math.Floor((length / w));
            List<PointCoord> res = new List<PointCoord>();
            for (int i = 1; i < k; i++)
            {
                double f = i * w / (length - i * w);
                PointCoord p = new PointCoord();
                p.X = (p1.X + f * p2.X) / (1 + f);
                p.Y = (p1.Y + f * p2.Y) / (1 + f);

                p.tagID = RID;
               /* if (p1.tagID != -1)
                {
                    p.SylWidth = p1.SylWidth;
                    p.tagID = p1.tagID;
                }
                else if (p2.tagID != -1)
                {
                    p.SylWidth = p2.SylWidth;
                    p.tagID = p2.tagID;
                }*/
                res.Add(p);
            }

            double fk = k * w / (length - k * w);
            PointCoord pk = new PointCoord();
            pk.X = (p1.X + fk * p2.X) / (1 + fk);
            pk.Y = (p1.Y + fk * p2.Y) / (1 + fk);
            pk.tagID = RID;
            length = (float)Math.Sqrt((pk.X - p2.X) * (pk.X - p2.X) + (pk.Y - p2.Y) * (pk.Y - p2.Y));

            if (length > 0.5 * w)
            {
                res.Add(pk);
            }

            return res;
        }
    }
}
