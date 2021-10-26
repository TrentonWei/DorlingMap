using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using System.IO;
using System.Data;
using AuxStructureLib.IO;

namespace AlgIterative
{
    /// <summary>
    /// 改进的最优梯度下降法
    /// </summary>
    public class AlgImprovedSGD
    {
         //简单数据结构数据
        public SDS MapSDS = null;
        //各类冲突权值
        public double PLCost = 10;
        public double PPCost = 5;
        public double DispCost = 1;
        public State State = null;
        public DispVectorTemplate DispTemplate = null;
        ConflictDetection ConflictDetector = null;

        public double DTol = 5;//移位范围，最大移位距离
        public double PPTol = 4;//面面之间距离阈值
        public double PLTol = 8;//面线之间距离阈值

        VoronoiDiagram VD = null; //V图
        SMap OMap = null;//原始地图
        SMap SMap = null;//SMap对象，保存当前状态
        SMap SMap1 = null;//SMap对象，保存当前状态，用于生V图

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="map">地图对象</param>
        /// <param name="plcost">线面冲突权值</param>
        /// <param name="ppcost">面面冲突权值</param>
        /// <param name="dcost">移位权重</param>
        /// <param name="dtol">移位距离阈值</param>
        /// <param name="pltol">面线距离阈值</param>
        /// <param name="pptol">面面距离阈值</param>
        /// <param name="i">迭代次数</param>
        public AlgImprovedSGD(SDS map, 
            double plcost, 
            double ppcost, 
            double dcost, 
            double dtol, 
            double pltol, 
            double pptol,
            VoronoiDiagram vd,
            SMap omap,
            SMap smap,
            SMap smap1)
        {
            MapSDS = map;

            PLCost = plcost;
            PPCost = ppcost;
            DispCost = dcost;

            VD = vd;
            OMap = omap;
            SMap = smap;//SMap对象，保存当前状态
            SMap1 = smap1;//SMap对象，保存当前状态，用于生V图

            DTol = dtol;
            PLTol = pltol;
            PPTol = pptol;

            State = new State();

            InitDispTemplate();
            this.DispTemplate.CreateIsTriableList1(VD, OMap, dtol);

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

            List<Conflict> conflictList = ConflictDetector.ConflictDetect(this.PLTol, this.PPTol);
            State.Conflicts = conflictList;
            int n = MapSDS.PolygonOObjs.Count;
            State.TrialPositionIDs = new int[n];
            for (int i = 0; i < n; i++)
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
                dx0 = Obj.TrialPosList[CurState.TrialPositionIDs[index]].Dx;
                dy0 = Obj.TrialPosList[CurState.TrialPositionIDs[index]].Dy;
                Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
            }

            dx = Obj.TrialPosList[Trial_PositionNO].Dx;
            dy = Obj.TrialPosList[Trial_PositionNO].Dy;
            Obj.Translate(dx, dy);

            //已经用了V图不需要以下判断了
            ////首先判断是否存在三角形的穿越
            //if (this.HasTriangleInverted(Obj))
            //{
            //    Obj.Translate((-1.0) * dx, (-1.0) * dy);
            //    Obj.Translate(dx0, dy0);
            //    return double.PositiveInfinity;
            //}

            ObjNewConflicts = this.ConflictDetector.ObjectConflictDetectPP_PL(Obj, this.PPTol, this.PLTol);
            double oNewCost = CostFunction(Obj, ObjNewConflicts, Trial_PositionNO);
            ObjCurConflicts = CurState.GetObjectConflict(Obj);
            double oCurCost = CostFunction(Obj, ObjCurConflicts, CurState.TrialPositionIDs[index]);
            double NewCost = CurState.State_Cost + (oNewCost - oCurCost);

            Obj.Translate((-1.0) * dx, (-1.0) * dy);//还原
            Obj.Translate(dx0, dy0);//还原

            return NewCost;
        }

        /// <summary>
        /// 计算总状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(State state)
        {
            double cost = 0;

            foreach (Conflict conflict in state.Conflicts)
            {
                if (conflict.ConflictType == @"PP")
                {

                    cost += (this.PPCost * (this.PPTol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {

                    cost += (this.PLCost * (this.PLTol - conflict.Distance));

                }
            }
            for (int i = 0; i < state.TrialPositionIDs.Length; i++)
            {

                cost += this.DispCost * this.MapSDS.PolygonOObjs[i].TrialPosList[state.TrialPositionIDs[i]].cost;
            }

            return cost;
        }

        /// <summary>
        /// 计算单个对象的状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(ISDS_MapObject Obj, List<Conflict> Conflicts, int PosIndex)
        {
            double cost = 0;

            if (Conflicts != null && Conflicts.Count > 0)
            {
                foreach (Conflict conflict in Conflicts)
                {
                    if (conflict.ConflictType == @"PP")
                    {

                        cost += (this.PPCost * (this.PPTol - conflict.Distance));
                    }
                    else if (conflict.ConflictType == @"PL")
                    {

                        cost += (this.PLCost * (this.PLTol - conflict.Distance));
                    }
                }
            }
            double d = (Obj as SDS_PolygonO).TrialPosList[PosIndex].cost;
            cost += this.DispCost * d;
            return cost;
        }




        /// <summary>
        /// 执行最陡坡降算法
        /// </summary>
        public double DoAlgSGD(string curPath, out int Init_No, out double Init_Cost)
        {
            int count = 1;
            #region 创建表格
            DataTable dtConflict = new DataTable("Time");
            dtConflict.Columns.Add("MoveID", typeof(int));
            dtConflict.Columns.Add("Conf_No", typeof(int));
            dtConflict.Columns.Add("Conf_Cost", typeof(double));
            #endregion
            double A = 0;
            double a=this.Evaluate_InitState();
            Init_No = this.State.Conflicts.Count;
            Init_Cost = this.State.State_Cost;
            A = a;
            double b=0;
            State cur_State = null;

            #region 添加初始冲突信息记录
            DataRow dr = dtConflict.NewRow();
            dr[0] = count;
            dr[1] = Init_No;
            dr[2] = Init_Cost;
            dtConflict.Rows.Add(dr);
            #endregion
            
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
                        int TriPosiCount = Obj.TrialPosList.Count;
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
                        count++;
                        //更新当前状态：状态值，位置表、冲突表、对象坐标
                        cur_State.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a, SMap, SMap1);

                        #region 添加冲突信息记录
                        dr = dtConflict.NewRow();
                        dr[0] = count;
                        dr[1] = this.State.Conflicts.Count;
                        dr[2] = this.State.State_Cost;
                        dtConflict.Rows.Add(dr);
                        #endregion
                    }
                }
            }

            //输出冲突信息表
            TXTHelper.ExportToTxt(dtConflict, curPath + @"\Conflicit.txt");

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
            }
        }
    }
}
