using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace RoadDisAlg
{
    /// <summary>
    /// 道路
    /// </summary>
    public class Road
    {
        public int FNode;                                 //起点
        public int TNode;                                 //终点
        public List<int> PointList = null;                //包含的顶点列表
        public List<RoadCurve> RoadCurveList = null;      //包含的弯曲段列表
        public RoadGrade RoadGrade;                        //图层ID
        public int RID;                                    //道路的编号
        public RoadNetWork NetWork;                        //道路所属的道路网

        /// <summary>
        /// 构造函数
        /// </summary>
        public Road(RoadGrade roadGrade,RoadNetWork netWork)
        {
            RoadGrade = roadGrade;
            NetWork = netWork;
            PointList = new List<int>();
            RoadCurveList = new List<RoadCurve>();
        }

        /// <summary>
        /// 获取该道路对应的Path对象
        /// </summary>
        public Polyline EsriPolyline
        {
            get
            { 
                object missing1 = Type.Missing;
                object missing2 = Type.Missing;
                int n=this.PointList.Count;
                Polyline polyline = null;
                if (n >= 2)
                {
                    polyline = new PolylineClass();
                    for (int i = 0; i < n; i++)
                    {
                        polyline.AddPoint(this.GetCoord(i).EsriPoint, ref missing1, ref missing2);
                    }
                    return polyline;
                }
                else
                    return null;
            }
        }
        /// <summary>
        /// 返回当前道路上第i个顶点的坐标
        /// </summary>
        /// <param name="i">顶点序号</param>
        /// <returns></returns>
        public PointCoord GetCoord(int i)
        {
            PointCoord point;
            try
            {
                point=this.NetWork.PointList[this.PointList[i]];
            }
            catch
            {
                return null;
            }
            return point;
        }
        /// <summary>
        /// 生成道路弯曲段列表（根据绕动方向算法）
        /// </summary>
        public void  CreateCurveList()
        {
            int n = PointList.Count;
            RoadCurve curCurve = null;
            if (n < 4)
            {
                curCurve = new RoadCurve(this);
                curCurve.PointList = this.PointList;

                curCurve.ComQulv();//计算曲率
                curCurve.Comab();   //计算形状参数值
  
                this.RoadCurveList.Add(curCurve);
                return;
            }
            else
            {
                int i=0;
                bool isStartCurve = true;
                float s=0f;
                while (i < n - 3)
                {
                    if (isStartCurve)
                    {
                        curCurve = new RoadCurve(this);
                        curCurve.PointList.Add(this.PointList[i]);
                        curCurve.PointList.Add(this.PointList[i + 1]);
                        curCurve.PointList.Add(this.PointList[i + 2]);
                        isStartCurve = false;
                    }
                    try
                    {
                        s = (float)(((GetCoord(i + 1).X - GetCoord(i).X) * (GetCoord(i + 2).Y - GetCoord(i + 1).Y) - (GetCoord(i + 2).X - GetCoord(i + 1).X) * (GetCoord(i + 1).Y - GetCoord(i).Y)) * ((GetCoord(i + 2).X - GetCoord(i + 1).X) * (GetCoord(i + 3).Y - GetCoord(i + 2).Y) - (GetCoord(i + 3).X - GetCoord(i + 2).X) * (GetCoord(i + 2).Y - GetCoord(i + 1).Y)));
                    }
                    catch
                    {
                        s = 0f;
                    }
                    if (s < 0)//绕动方法改变的情况
                    {
                        curCurve.ComQulv();//计算曲率
                        curCurve.Comab();   //计算形状参数值
                        this.RoadCurveList.Add(curCurve);
                        i = i + 2;
                        isStartCurve = true;
                    }
                    else//绕动方法不变的情况
                    {
                        curCurve.PointList.Add(this.PointList[i + 3]);
                        i = i + 1;
                    }
                }

                if (i >= n - 3 && s < 0)
                {
                    curCurve = new RoadCurve(this);
                    for (int j = i; j < n; j++)
                    {
                        curCurve.PointList.Add(this.PointList[j]);
                    }
                    curCurve.ComQulv();//计算曲率
                    curCurve.Comab();   //计算形状参数值
                    this.RoadCurveList.Add(curCurve);
                }
                else
                {
                    curCurve.ComQulv();//计算曲率
                    curCurve.Comab();   //计算形状参数值
                    this.RoadCurveList.Add(curCurve);
                }
            }
        }
    }
}
