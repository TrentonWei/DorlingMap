using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{
    [Serializable]
    public abstract class MapObject
    {
        public int ID=-1;
        public double SylWidth =0;
        public int TypeID = 1;//1线要素，2边界
        public int SomeValue=-1;

        public int ConflictCount = 0;
        

        /// <summary>
        /// 获取类型
        /// </summary>
        public abstract FeatureType FeatureType
        {
            get;
        }

    }
    /// <summary>
    /// 点目标
    /// </summary>
    [Serializable]
    public class PointObject : MapObject
    {
        public TriNode Point= null;

        public override FeatureType FeatureType
        {
            get { return FeatureType.PointType; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="point">点坐标</param>
        public PointObject(int id, TriNode point)
        {
            ID = id;
            Point = point;
        }

        /// <summary>
        /// 通过ID号获取点目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PointObject GetPPbyID(List<PointObject> PList, int ID)
        {
            foreach (PointObject curP in PList)
            {
                if (curP.ID == ID)
                    return curP;
            }
            return null;
        }


        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            //throw new NotImplementedException();
            return new ProxiNode(this.Point.X, this.Point.Y, -1, this.ID,FeatureType.PointType);
        }
    }

    /// <summary>
    /// 线目标
    /// </summary>
    [Serializable]
    public class PolylineObject : MapObject
    {
        //public int ID = -1;
        public List<TriNode> PointList = null;    //节点列表


        public override FeatureType FeatureType
        {
            get { return FeatureType.PolylineType; }
        }

        public PolylineObject(int id)
        {
            ID = id;
        }

        public PolylineObject()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="pointList">线列表</param>
        /// <param name="width">符号宽度</param>
        public PolylineObject(int id, List<TriNode> pointList, double width)
        {
            ID = id;
            PointList = pointList;
            this.SylWidth = width;
        }

        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolylineObject GetPLbyID(List<PolylineObject> PLList,int ID)
        {
            foreach (PolylineObject curPL in PLList)
            {
                if (curPL.ID == ID)
                    return curPL;
            }
            return null;
        }

        public IPolyline ToEsriPolyline()
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IPolyline esriPolyline = new PolylineClass();
            // shp.SpatialReference = mapControl.SpatialReference;
            IPointCollection pointSet = esriPolyline as IPointCollection;
            IPoint curResultPoint = null;
            TriNode curPoint = null;
            if (this == null)
                return null;
            int m = this.PointList.Count;

            for (int k = 0; k < m; k++)
            {
                curPoint = this.PointList[k];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
            }
            return esriPolyline;
        }
        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            double sumx = 0;
            double sumy = 0;
            foreach (TriNode curP in this.PointList)
            {
                sumx += curP.X;
                sumy += curP.Y;
            }
            sumx = sumx / this.PointList.Count;
            sumy = sumy / this.PointList.Count;
            return new ProxiNode(sumx, sumy, -1, this.ID, FeatureType.PolylineType); 
        }
    }
    /// <summary>
    /// 构造函数
    /// </summary>
    public class ConNode : MapObject
    {
       // public int ID = -1;
       // public double SylWidth = -1;              //以毫米为单位的符号宽度
        public TriNode Point = null;

        public override FeatureType FeatureType
        {
            get { return FeatureType.Unknown; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="width"></param>
        /// <param name="point"></param>
        public ConNode(int id, double width, TriNode point)
        {
            ID = id;
            this.SylWidth = width;
            Point = point;
        }
        /// <summary>
        /// 获取ID的结点
        /// </summary>
        /// <param name="NList"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ConNode GetPLbyID(List<ConNode> NList, int ID)
        {
            foreach (ConNode curNL in NList)
            {
                if (curNL.ID == ID)
                    return curNL;
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<ConNode> NList, List<TriNode> vexList,double x, double y)
        {
            
            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (ConNode curNode in NList)
            {
               // int id = curNode.ID;
                TriNode curV = curNode.Point;

                if (Math.Abs((1-curV.X/x)) <= 0.000001f && Math.Abs((1-curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<TriNode> NList,  double x, double y)
        {

            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (TriNode curNode in NList)
            {
                // int id = curNode.ID;
                TriNode curV = curNode;

                if (Math.Abs((1 - curV.X / x)) <= 0.00001f && Math.Abs((1 - curV.Y / y)) <= 0.00001f)
                {
                    return curV;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<ConNode> NList, double x, double y)
        {

            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (ConNode curNode in NList)
            {
                // int id = curNode.ID;
                TriNode curV = curNode.Point;

                if (Math.Abs((1 - curV.X / x)) <= 0.000001f && Math.Abs((1 - curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }


    }

    /// <summary>
    /// 面目标
    /// </summary>
    [Serializable]
    public class PolygonObject : MapObject
    {
        public double R;//表示对应Circle的半径
       // public int ID = -1;
        public List<TriNode> PointList = null;    //节点列表

        public List<TrialPosition> TriableList = null;    //节点列表
        private double area=-1;
        private double perimeter = -1;
        public override FeatureType FeatureType
        {
            get { return FeatureType.PolygonType; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="pointList">线列表</param>
        public PolygonObject(int id, List<TriNode> pointList)
        {
            ID = id;
            PointList = pointList;
            TriableList = new List<TrialPosition>();
        }

        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolygonObject GetPPbyID(List<PolygonObject> PPList, int ID)
        {
            foreach (PolygonObject curPP in PPList)
            {
                if (curPP.ID == ID)
                    return curPP;
            }
            return null;
        }
        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            double sumx = 0;
            double sumy = 0;
            foreach (TriNode curP in this.PointList)
            {
                sumx += curP.X;
                sumy += curP.Y;
            }
            sumx = sumx / this.PointList.Count;
            sumy = sumy / this.PointList.Count;
            return new ProxiNode(sumx, sumy, -1, this.ID, FeatureType.PolygonType); 
        }

        /// <summary>
        /// 计算多边形面积
        /// </summary>
        /// <returns></returns>
        public double Area
        {
            get
            {
                if (this.area != -1)
                    return this.area;
                else
                {
                    this.area = 0;
                    int n = this.PointList.Count;
                    this.PointList.Add(PointList[0]);
                    for (int i = 0; i < n; i++)
                    {
                        area += (PointList[i].X * PointList[i + 1].Y - PointList[i + 1].X * PointList[i].Y);
                       
                    }
                    area = 0.5*Math.Abs(area);
                    this.PointList.RemoveAt(n);
                    return area;
                }
            }
        }

        /// <summary>
        /// 面平移
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Translate(double dx, double dy)
        {
            if (dx != 0 || dy != 0)
            {
                foreach (TriNode vetex in this.PointList)
                {
                    vetex.X += dx;
                    vetex.Y += dy;
                }
            }
        }

        /// <summary>
        /// 计算多边形周长
        /// </summary>
        /// <returns></returns>
        public double Perimeter
        {
            get
            {
                if (this.perimeter != -1)
                    return this.perimeter;
                else
                {
                    this.perimeter = 0;
                    int n = this.PointList.Count;
                    for (int i = 0; i < n - 1; i++)
                    {
                        perimeter += ComFunLib.CalLineLength(PointList[i], PointList[i + 1]);
                    }
                    perimeter += ComFunLib.CalLineLength(PointList[n-1], PointList[0]);
                    return perimeter;
                }
            }
        }

    }
}
