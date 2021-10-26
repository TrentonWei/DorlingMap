using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.IO;

namespace AlgIterative
{
    /// <summary>
    /// 最陡坡降法
    /// </summary>
    public class AlgSGD
    {
        //简单数据结构数据
        public SDS MapSDS = null;
        //各类冲突权值
        public double PLCost = 10;
        public double PPCost = 5;
        public double DispCost = 1;
        public double DTol = 5;
        public State State = null;
        public DispVectorTemplate DispTemplate = null;
        ConflictDetection ConflictDetector = null;

        public AlgSGD(SDS map)
        {
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);
            
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="plcost">线面冲突权值</param>
        /// <param name="ppcost">面面冲突权值</param>
        /// <param name="dcost">移位权重</param>
        /// <param name="dtol">距离阈值</param>
        public AlgSGD(SDS map,double plcost,double ppcost,double dcost,double dtol)
        {
            PLCost = plcost;
            PPCost = ppcost;
            DispCost = dcost;
            DTol = dtol;
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);

        }

      

        /// <summary>
        /// 初始化移位向量模版-16方向
        /// </summary>
        private void InitDispTemplate()
        {
            List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
            TrialPosiDisGroup g1 = new TrialPosiDisGroup(DTol / 4.0, 0, Math.PI / 2);
            TrialPosiDisGroup g2 = new TrialPosiDisGroup(DTol / 2.0, Math.PI / 8, Math.PI / 4);
            TrialPosiDisGroup g3 = new TrialPosiDisGroup(3.0*DTol / 4.0, 0, Math.PI / 8);
            TrialPosiDisGroup g4 = new TrialPosiDisGroup(DTol, 0, Math.PI / 16);

            TPDGList.Add(g1);
            TPDGList.Add(g2);
            TPDGList.Add(g3);
            TPDGList.Add(g4);
            DispTemplate = new DispVectorTemplate(TPDGList);
            DispTemplate.CalTriPosition();
        }

        /// <summary>
        /// 计算初始目标值
        /// 思路：找出所有冲突，然后移位
        /// </summary>
        /// <returns></returns>
        public double Evaluate_InitState()
        {
            List<Conflict> conflictList = ConflictDetector.ConflictDetect(DTol);
            State.Conflicts = conflictList;
            int n = MapSDS.PolygonOObjs.Count;
            State.TrialPositionIDs = new int[n];
            for (int i = 0; i < n;i++ )
            {
                State.TrialPositionIDs[i] = 0;
            }
            this.State.State_Cost = CostFunction(State);
            return this.State.State_Cost;
        }

        /// <summary>
        /// 通过更改一个位置计算新的目标值
        /// 思路：先将CurState中与Obj相关的所有冲突揪出来，然后重新计算Obj的冲突
        /// 两者对比评价值，他们的差别即是两个不同状态的差别
        /// </summary>
        /// <param name="CurState">当前状态</param>
        /// <param name="Obj">当前假设要移位的对象</param
        /// <param name="Trial_PositionNO">当前试探的新位置</param>
        /// <param name="ObjCurConflicts">对象当前涉及的冲突（输出）</param>
        /// <param name="ObjNewConflicts">假设对象移位后涉及的冲突（输出）</param>
        /// <param name="index">对象的索引号（在所有对象中的排列号）（输出）</param>
        /// <param name="dx">移位X（输出）</param>
        /// <param name="dy">移位Y（输出）</param>
        /// <returns>新的评价值</returns>
        private double Evaluate_NewState(State CurState, SDS_PolygonO Obj, int Trial_PositionNO,
            out List<Conflict>  ObjCurConflicts,out List<Conflict> ObjNewConflicts,
            out int index ,out double dx,out double dy ,out double dx0, out double dy0)
        {

            index = 0;
            dy0 = 0;
            dx0 = 0;
            ObjCurConflicts = null;
            ObjNewConflicts = null;
            //找到对象在数组中的位置
            for (index = 0; index < this.MapSDS.PolygonOObjs.Count; index++)
            {
                if (this.MapSDS.PolygonOObjs[index] == Obj)
                    break;
            }

            if (CurState.TrialPositionIDs[index] != 0)
            {
                dx0 = DispTemplate.TrialPosiList[CurState.TrialPositionIDs[index]].Dx;
                dy0 = DispTemplate.TrialPosiList[CurState.TrialPositionIDs[index]].Dy;
                Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
            }

            dx=this.DispTemplate.TrialPosiList[Trial_PositionNO].Dx;
            dy=this.DispTemplate.TrialPosiList[Trial_PositionNO].Dy;
            Obj.Translate(dx, dy);


            //首先判断是否存在三角形的穿越
            if (this.HasTriangleInverted(Obj))
            {
                 Obj.Translate((-1.0)*dx,(-1.0)* dy);
                 Obj.Translate(dx0, dy0);
                 return CurState.State_Cost;
            }

            ObjNewConflicts = this.ConflictDetector.ObjectConflictDetect(Obj, this.DTol);
            double oNewCost = CostFunction(ObjNewConflicts, Trial_PositionNO);
            ObjCurConflicts=CurState.GetObjectConflict(Obj);
            double oCurCost = CostFunction(ObjCurConflicts, CurState.TrialPositionIDs[index]);
            double NewCost=CurState.State_Cost+(oNewCost-oCurCost);
           
            Obj.Translate((-1.0)*dx,(-1.0)* dy);//还原
            Obj.Translate(dx0, dy0);//还原

            return NewCost;
        }

        /// <summary>
        /// 判断是否存在三角形的穿越
        /// </summary>
        /// <param name="Polygon">多边i型你</param>
        /// <returns></returns>
        /// 
        private bool HasTriangleInverted(SDS_PolygonO Polygon)
        {
            List<SDS_Triangle> triList = Polygon.GetSurroundingTris();
            foreach (SDS_Triangle curTri in triList)
            {
                if (!curTri.IsAnticlockwise())
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 计算总状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(State state)
        {
            double cost=0;

            //foreach (Conflict conflict in state.Conflicts)
            //{
            //    if (conflict.ConflictType == @"PP")
            //    {
            //        cost += (this.PPCost * (this.DTol - conflict.Distance));
            //    }
            //    else if (conflict.ConflictType == @"PL")
            //    {
            //        cost += (this.PLCost * (this.DTol - conflict.Distance));
            //    }
            //}
            //for(int i=0;i<state.TrialPositionIDs.Length;i++)
            //{
            //    if (state.TrialPositionIDs[i] != 0)
            //    {
            //        cost += this.DispCost*DispTemplate.TrialPosiList[i].Distance;
            //       // cost += 1;
            //    }
            //}

            foreach (Conflict conflict in state.Conflicts)
            {
                if (conflict.ConflictType == @"PP")
                {
                    //cost += this.PPCost;
                    cost += (this.PPCost * (this.DTol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {
                   
                    //cost += this.PLCost;
                    cost += (this.PLCost * (this.DTol - conflict.Distance));

                }
            }
            for (int i = 0; i < state.TrialPositionIDs.Length; i++)
            {
                if (state.TrialPositionIDs[i] != 0)
                {
                    cost += this.DispCost * DispTemplate.TrialPosiList[state.TrialPositionIDs[i]].Distance;
                    // cost += 1;
                }
            }

            return cost;
        }

        /// <summary>
        /// 计算单个对象的状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(List<Conflict> Conflicts,int PosIndex)
        {
            double cost = 0;
            //if (Conflicts != null && Conflicts.Count > 0)
            //{
            //    foreach (Conflict conflict in Conflicts)
            //    {
            //        if (conflict.ConflictType == @"PP")
            //        {
            //            //cost += 5;
            //            cost += (this.PPCost *(this.DTol- conflict.Distance));
            //        }
            //        else if (conflict.ConflictType == @"PL")
            //        {
            //            //cost += 10;
            //            cost += (this.PLCost * (this.DTol - conflict.Distance));
            //        }
            //    }
            //}
            //double d = this.DispTemplate.TrialPosiList[PosIndex].Distance;
            //cost += this.DispCost*d;

            if (Conflicts != null && Conflicts.Count > 0)
            {
                foreach (Conflict conflict in Conflicts)
                {
                    if (conflict.ConflictType == @"PP")
                    {
                       // cost += this.PPCost;
                        cost += (this.PPCost * (this.DTol - conflict.Distance));
                    }
                    else if (conflict.ConflictType == @"PL")
                    {
                        //cost += this.PLCost;
                        cost += (this.PLCost * (this.DTol - conflict.Distance));
                    }
                }
            }
            double d = this.DispTemplate.TrialPosiList[PosIndex].Distance;
            cost += this.DispCost * d;
            return cost;
        }




        /// <summary>
        /// 执行最陡坡降算法
        /// </summary>
        public double DoAlgSGD(out int ConflictN,StreamWriter sw)
        {
            double A = 0;
            double a=this.Evaluate_InitState();
            ConflictN=this.State.Conflicts.Count;
            WriteConflictTxt(sw,this.State.Conflicts);
            A = a;
            double b=0;
            State cur_State = null;
            int TriPosiCount = this.DispTemplate.TrialPosiList.Count;

            if (a == 0)//或小于摸个阈值
                return 0; 
            else
            {
                bool IsImproved=true;
                cur_State = this.State;
                SDS_PolygonO move_Obj = null;
                int move_Pos = -1;
                List<Conflict> oNewConflict=null;//对象新的冲突
                List<Conflict> oCurConflict=null;//对象当前冲突
                int index=-1;//对象在数组中的位置
                double Dx = -1;
                double Dy = -1;
                double Dx0 = -1;
                double Dy0 = -1; 
                while (IsImproved)
                {
                    IsImproved = false;
                    foreach (SDS_PolygonO Obj in this.MapSDS.PolygonOObjs)
                    {
                        for (int i = 0; i < TriPosiCount; i++)
                        {
                            List<Conflict> oNewC=null;//对象新的冲突
                            List<Conflict> oCurC=null;//对象当前冲突
                            int id;
                            double dx;
                            double dy;
                            double dx0;
                            double dy0;
                            b = this.Evaluate_NewState(cur_State, Obj, i, out oCurC, out oNewC, out id, out dx, out dy,out dx0,out dy0);
                            if (b < a)
                            {
                                move_Obj = Obj;
                                move_Pos = i;
                                a = b;
                                oNewConflict=oNewC;
                                oCurConflict=oCurC;
                                IsImproved = true;
                                index=id;
                                Dx = dx;
                                Dy = dy;
                                Dx0 = dx0;
                                Dy0 = dy0; 
                            }
                        }
                    }
                    if (IsImproved)
                    {
                        //更新当前状态：状态值，位置表、冲突表、对象坐标
                        cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0,Dy0,oNewConflict, oCurConflict,a);
                    }
                }
            }

            WriteConflictTxt(sw, this.State.Conflicts);

            return A;
        }
        /// <summary>
        /// 写文本文件
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="cl"></param>
        private void WriteConflictTxt(StreamWriter sw,List<Conflict> cl)
        {
            sw.Write("InitConflict:" + cl.Count);
            sw.WriteLine();
            sw.Write("ID" + "  " + "OID1" + "  " + "OT1" + "  " + "OID2" + "  " + "OT2");
            sw.WriteLine();
            for (int i=0;i<cl.Count;i++)
            {
                sw.Write(i.ToString() + "  " +
                    cl[i].Obj1.AID.ToString() +
                    "  " + cl[i].Obj1.ObjType.ToString() +
                    "  " + cl[i].Obj2.AID.ToString() +
                    "  " + cl[i].Obj2.ObjType.ToString());
                sw.WriteLine();
                //sw.Close();
            }

        }
        
    }
}


