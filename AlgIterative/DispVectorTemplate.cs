using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace AlgIterative
{
    /// <summary>
    /// 里原始位置同样距离的一层位置的表达
    /// 是一个（距离、起始角度、间隔角度）的三元组
    /// </summary>
    public class TrialPosiDisGroup
    {
        public double StartAngle;
        public double IntervalofAngle ;
        public double Distance;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Distantce">距离</param>
        /// <param name="StartAngle">起始角度</param>
        /// <param name="IntervalofAngle">间隔角度</param>
        public TrialPosiDisGroup(double distance, double startAngle, double intervalofAngle)
        {
            Distance = distance;
            StartAngle = startAngle;
            IntervalofAngle = intervalofAngle;
        }
    }
    ///// <summary>
    ///// 候选状态位置
    ///// </summary>
    //public class TrialPosition
    //{
    //   public int ID;
    //   public double Distance;
    //   public double Dx;
    //   public double Dy;
    //   public double Angle;
    //    /// <summary>
    //    /// 构造函数
    //    /// </summary>
    //    /// <param name="id">ID号</param>
    //    /// <param name="distance">距离</param>
    //    /// <param name="dx">X方向偏移</param>
    //    /// <param name="dy">Y方向偏移</param>
    //    /// <param name="angle">角度</param>
    //    public TrialPosition(int id, double distance, double dx, double dy, double angle)
    //    {
    //        ID = id;
    //        Distance = distance;
    //        Dx = dx;
    //        Dy = dy;
    //        Angle = angle;
    //    }
    //}

    /// <summary>
    /// 同一角度上的位置组成的列表
    /// </summary>
    public class TrialPosiAngleGroup
    {
        public double Angle;
        public List<TrialPosition> TrialPositionList; //该角度上的TPID列表;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Distantce">距离</param>
        /// <param name="StartAngle">起始角度</param>
        /// <param name="IntervalofAngle">间隔角度</param>
        public TrialPosiAngleGroup( double angle)
        {
            Angle = angle;
            TrialPositionList = new List<TrialPosition>();
        }
    }

    /// <summary>
    /// 移位模版-定义所有的移位试探位置（Displaced state trial positions）
    /// </summary>
    public class DispVectorTemplate
    {
       public List<TrialPosiDisGroup> TrialPosiDisGroupList = null;//输入的距离组

       public List<TrialPosition> TrialPosiList = null;//移位试探位置
       public List<TrialPosiAngleGroup> AngleGroupList = null;//角度组列表

        /// <summary>
        /// 构造函数
        /// </summary>
        public DispVectorTemplate(List<TrialPosiDisGroup> trialPosiDisGroupList)
        {
           TrialPosiDisGroupList = trialPosiDisGroupList;//输入的距离组
           TrialPosiList = new List<TrialPosition> ();//移位试探位置
           AngleGroupList = new List<TrialPosiAngleGroup> ();//角度组列表
        }

        /// <summary>
        /// 根据TrialPosiGroup计算出一组位置
        /// </summary>
        /// <param name="tg">TrialPosiGroup对象</param>
        /// <param name="startID">起始ID</param>
        /// <returns>位置列表</returns>
        public void CalTriPosition()
        {
            if (TrialPosiDisGroupList == null || TrialPosiDisGroupList.Count == 0)
                return;
            int curID = 1;
            double curDx = 0;
            double curDy = 0;
            curDx = 0;
            curDy = 0;
            TrialPosition curTP = new TrialPosition(0, 0, 0, 0, 0);
            this.TrialPosiList.Add(curTP);
            TrialPosiAngleGroup angleGroup = null;
            foreach (TrialPosiDisGroup curTg in TrialPosiDisGroupList)
            {
                double curD=curTg.Distance;
                for (double curAngle = curTg.StartAngle; curAngle < 2 * Math.PI; curAngle = curAngle + curTg.IntervalofAngle)
                {
                    curDx = curD * Math.Cos(curAngle);
                    curDy = curD * Math.Sin(curAngle);
                    curTP = new TrialPosition(curID, curD, curDx, curDy, curAngle);
                    this.TrialPosiList.Add(curTP);
                    angleGroup =GetAngleGroup(curAngle);
                    angleGroup.TrialPositionList.Add(curTP);
                }
            }
        }

        /// <summary>
        /// 获取角度为angle的分组，如果没有则创建并加入列表中
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>返回角度组</returns>
        private TrialPosiAngleGroup GetAngleGroup(double angle)
        {
            if (this.AngleGroupList == null)
                return null;
            foreach (TrialPosiAngleGroup angleGroup in this.AngleGroupList)
            {
                if (Math.Abs(angleGroup.Angle-angle)<0.01)
                {
                    return angleGroup;
                }
            }
            TrialPosiAngleGroup newAngleGroup = new TrialPosiAngleGroup(angle);
            this.AngleGroupList.Add(newAngleGroup);
            return newAngleGroup;
        }

        /// <summary>
        /// 创建可否试探bool标识列表
        /// </summary>
        /// <param name="vd"></param>
        public void CreateIsTriableList(VoronoiDiagram vd)
        {
            foreach (VoronoiPolygon vp in vd.VorPolygonList)
            {
                foreach (TrialPosition tp in this.TrialPosiList)
                {

                    if (vp.IsPolygonObjectInPolygon(tp.Dx, tp.Dy))
                    {
                        (vp.MapObj as PolygonObject).TriableList.Add(tp);
                    }

                }
            }
        }


        /// <summary>
        /// 创建可否试探bool标识列表
        /// </summary>
        /// <param name="vd"></param>
        public void CreateIsTriableList1(VoronoiDiagram vd, SMap originalMap,double DisTol)
        {
            foreach (VoronoiPolygon vp in vd.VorPolygonList)
            {
                PolygonObject cP= vp.MapObj as PolygonObject;
                cP.TriableList.Clear();
                int id =cP.ID;
                PolygonObject oo = originalMap.GetObjectbyID(id, FeatureType.PolygonType) as PolygonObject;
                Node op = oo.CalProxiNode();
                Node cp = cP.CalProxiNode();
               
                foreach (TrialPosition tp in this.TrialPosiList)
                {
                    Node np = new TriNode(cp.X+tp.Dx,cp.Y+tp.Dy);

                    double dis = Math.Sqrt((op.X - np.X) * (op.X - np.X) + (op.Y - np.Y) * (op.Y - np.Y));
                    if (dis <= DisTol)
                    {
                        tp.cost = dis;
                        if (vp.IsPolygonObjectInPolygon(tp.Dx, tp.Dy))
                        {
                            cP.TriableList.Add(tp);
                        }
                    }
                }
            }
        }
    }
}
