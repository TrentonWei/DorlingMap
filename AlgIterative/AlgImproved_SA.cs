using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace AlgIterative
{
    public  class AlgImproved_SA
    {
        //简单数据结构数据
        public SDS MapSDS = null;
        //各类冲突权值
        public double PLCost = 10;
        public double PPCost = 1;
        public double DispCost = 1;
        public double DTol = 7.5;

        public State State=null;
        public double T0=1000;
        public double X=0.99;
        public int M=100;
        public int N= 100;

        ConflictDetection ConflictDetector = null;
        public DispVectorTemplate DispTemplate = null;//模版

        public AlgImproved_SA(SDS map)
        {
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);
        }
       /// <summary>
       /// 
       /// </summary>
       /// <param name="map">地图对象</param>
        /// <param name="plCost">线-面冲突权重</param>
        /// <param name="ppCost">面-面冲突权重</param>
        /// <param name="dispCost">移位距离权重</param>
        /// <param name="dtol">最小间距阈值</param>
        /// <param name="T0">初始温度</param>
        /// <param name="X">温度降温速率</param>
        /// <param name="M">抽样终止条件</param>
        /// <param name="N">迭代终止条件</param>
        public AlgImproved_SA(SDS map,//地图对象
                   double plCost,//线-面冲突权重
                   double ppCost,//面-面冲突权重
                   double dispCost,//移位距离权重
                   double dtol,//最小间距阈值
                   double t0,//初始温度
                   double x,//温度降温速率
                   int m,  //抽样终止条件
                   int n   //迭代终止条件
            )
        {
            State = new State();
            InitDispTemplate();
            MapSDS = map;
            ConflictDetector = new ConflictDetection(MapSDS);
            PLCost = plCost;
            PPCost = ppCost;
            DispCost = dispCost;
            DTol = dtol;
            T0 = t0;
            X = x;
            M = m;
            N = n;
        }

        /// <summary>
        /// 初始化移位向量模版-16方向
        /// </summary>
        private void InitDispTemplate()
        {
            List<TrialPosiDisGroup> TPDGList = new List<TrialPosiDisGroup>();
            TrialPosiDisGroup g1 = new TrialPosiDisGroup(this.DTol / 3.0, 0, Math.PI / 2);
            TrialPosiDisGroup g2 = new TrialPosiDisGroup((2 * this.DTol) / 3.0, Math.PI / 8, Math.PI / 4);
            TrialPosiDisGroup g3 = new TrialPosiDisGroup(DTol, 0, Math.PI / 8);
            // TrialPosiDisGroup g4= new TrialPosiDisGroup(5 , 0, Math.PI / 16);
            TPDGList.Add(g1);
            TPDGList.Add(g2);
            TPDGList.Add(g3);
            //TPDGList.Add(g4);
            DispTemplate = new DispVectorTemplate(TPDGList);
            DispTemplate.CalTriPosition();
        }


        /// <summary>
        /// 抽样
        ///（1）令k=0时的初始当前状态为s’(0)=s(i)，q=0；
        ///（2）由状态s通过状态产生函数产生新状态s’，计算增量∆C’=C(s’)-C(s)；
        ///（3）若∆C’<0，则接受s’作为当前解，并判断C(s’)< C(s*’) ?若是，则令s*’=s’，q=0；否则，令q=q+1。若∆C’>0，则以概率exp(-∆C’/t)接受s’作为下一当前状态；
        ///（4）令k=k+1，判断q>m1? 若是，则转第(5)步；否则，返回第(2)步；
        ///（5）将当前最优解s*’和当前状态s’(k)返回改进退火过程。
        /// </summary>
        /// <param name="t">温度</param>
        /// <param name="curState">当前状态</param>
        /// <param name="bestState">当前最优状态</param>
        /// <param name="M">终止条件，当在curState的邻域下经M次移动无法得到更优的目标值时结束本次抽样</param>
        public void DoSampling(double t, ref State curState,out State bestState,int M)
        {
            int q = 0;
            double a = curState.State_Cost;
            bestState = curState.Clone() as State;
            double b = a;
            double dE=0;
            State newState = null;

            SDS_PolygonO move_Obj = null;
            int move_Pos = -1;
            List<Conflict> oNewConflict = null;//对象新的冲突
            List<Conflict> oCurConflict = null;//对象当前冲突
            int index = -1;//对象在数组中的位置
            double Dx = -1;
            double Dy = -1;
            double Dx0 = 0;
            double Dy0 = 0;

            while (q < M)
            {
                int indexofObj;
                SDS_PolygonO curObj;
                int indexofPos;
                newState = Generate_New_State(curState,
                    out indexofObj,
                    out curObj,
                    out indexofPos);

                List<Conflict> oNewC = null;//对象新的冲突
                List<Conflict> oCurC = null;//对象当前冲突
                int id;
                double dx;
                double dy;
                double dx0;
                double dy0;
                bool isInverted;
                b = this.Evaluate_NewState(newState,
                                           curObj, indexofPos,
                                           out oCurC, out oNewC,
                                           out  id,
                                           out  dx, out  dy,
                                           out  dx0, out  dy0,
                                           out isInverted);
                if (isInverted == true)//三角形穿越
                    continue;
                dE = b - a;
                if (dE < 0)
                {
                    curState = newState;
                    a = b;
                    move_Obj = curObj;
                    move_Pos = indexofPos;

                    oNewConflict = oNewC;
                    oCurConflict = oCurC;
                    index = id;
                    Dx = dx;
                    Dy = dy;
                    Dx0 = dx0;
                    Dy0 = dy0;
                    //Reture2OriginalPos(move_Obj, cur_State);
                    curState.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                    if (bestState.State_Cost > a)
                    {
                        bestState = curState.Clone() as State;
                        q = 0;  
                    }
                    q++;
                    
                }
                else
                {
                        double P =Math.Exp((-1.0) * dE / t);
                        double R = this.GenerateRandomFoat();
                        if (R < P)
                        {
                            curState = newState;
                            a = b;
                            move_Obj = curObj;
                            move_Pos = indexofPos;

                            oNewConflict = oNewC;
                            oCurConflict = oCurC;
                            index = id;
                            Dx = dx;
                            Dy = dy;
                            Dx0 = dx0;
                            Dy0 = dy0;
                            //Reture2OriginalPos(move_Obj, cur_State);
                            curState.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                         
                        }
                        q++;
                }
            }
        }

        public void DoSampling1(double t, ref State curState, out State bestState, int M)
        {
            int q = 0;
            double a = curState.State_Cost;
            bestState = curState.Clone() as State;
            double b = a;
            double dE = 0;
            //State newState = null;

            SDS_PolygonO move_Obj = null;
            int move_Pos = -1;
            List<Conflict> oNewConflict = null;//对象新的冲突
            List<Conflict> oCurConflict = null;//对象当前冲突
            int index = -1;//对象在数组中的位置
            double Dx = -1;
            double Dy = -1;
            double Dx0 = 0;
            double Dy0 = 0;

            while(q<M) 
            {
                int indexofObj;
                SDS_PolygonO curObj;
                int indexofPos;

                indexofObj = this.GenerateRandomNumber(this.MapSDS.PolygonOObjs.Count);
                curObj = MapSDS.PolygonOObjs[indexofObj];
                indexofPos = this.GenerateRandomNumber(this.DispTemplate.TrialPosiList.Count);

                List<Conflict> oNewC = null;//对象新的冲突
                List<Conflict> oCurC = null;//对象当前冲突
                int id;
                double dx;
                double dy;
                double dx0;
                double dy0;
                bool isInverted;
                b = this.Evaluate_NewState(curState,
                                           curObj, indexofPos,
                                           out oCurC, out oNewC,
                                           out  id,
                                           out  dx, out  dy,
                                           out  dx0, out  dy0,
                                           out isInverted);
                if (isInverted == true)//三角形穿越
                    continue;
                dE = b - a;
                if (dE < 0)
                {
                    
                    a = b;
                    move_Obj = curObj;
                    move_Pos = indexofPos;

                    oNewConflict = oNewC;
                    oCurConflict = oCurC;
                    index = id;
                    Dx = dx;
                    Dy = dy;
                    Dx0 = dx0;
                    Dy0 = dy0;
                    //Reture2OriginalPos(move_Obj, cur_State);
                    curState.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                    if (bestState.State_Cost > a)
                    {
                        bestState = curState.Clone() as State;
                        q = 0;
                    }
                    else
                    {
                        q++;
                    }

                }
                else if (dE == 0)
                    continue;
                else
                {
                    double P = Math.Exp((-1.0) * dE / t);

                    double R = this.GenerateRandomFoat();
                    if (R < P)
                    {
                        
                        a = b;
                        move_Obj = curObj;
                        move_Pos = indexofPos;

                        oNewConflict = oNewC;
                        oCurConflict = oCurC;
                        index = id;
                        Dx = dx;
                        Dy = dy;
                        Dx0 = dx0;
                        Dy0 = dy0;
                        //Reture2OriginalPos(move_Obj, cur_State);
                        curState.UpdateState(index, move_Obj, move_Pos, Dx, Dy, Dx0, Dy0, oNewConflict, oCurConflict, a);
                       
                    }
                    q++;
                }
            }
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
        /// <param name="dx0">移位X（输出）</param>
        /// <param name="dy0">移位Y（输出）</param>
        /// <returns>新的评价值</returns>
        private double Evaluate_NewState(State curState, 
            SDS_PolygonO Obj, int Trial_PositionNO,
            out List<Conflict> ObjCurConflicts, 
            out List<Conflict> ObjNewConflicts,
            out int index,
            out double dx, out double dy, 
            out double dx0, out double dy0,
            out bool isInverted)
        {
            isInverted = false;

            dx0 = 0;
            dy0 = 0;
            index = 0;
            ObjCurConflicts = null;
            ObjNewConflicts = null;
            //找到对象在数组中的位置
            for (index = 0; index < this.MapSDS.PolygonOObjs.Count; index++)
            {
                if (this.MapSDS.PolygonOObjs[index] == Obj)
                    break;
            }
            if (curState.TrialPositionIDs[index] != 0)
            {
                dx0 = DispTemplate.TrialPosiList[curState.TrialPositionIDs[index]].Dx;
                dy0 = DispTemplate.TrialPosiList[curState.TrialPositionIDs[index]].Dy;
                Obj.Translate((-1.0) * dx0, (-1.0) * dy0);
            }

            dx = this.DispTemplate.TrialPosiList[Trial_PositionNO].Dx;
            dy = this.DispTemplate.TrialPosiList[Trial_PositionNO].Dy;
            Obj.Translate(dx, dy);

            //首先判断是否存在三角形的穿越
            if (this.HasTriangleInverted(Obj))
            {
                Obj.Translate((-1.0) * dx, (-1.0) * dy);
                Obj.Translate(dx0, dy0);
                isInverted = true;
                return curState.State_Cost;
            }

            ObjNewConflicts = this.ConflictDetector.ObjectConflictDetect(Obj, this.DTol);
            double oNewCost = CostFunction(ObjNewConflicts, Trial_PositionNO);
            ObjCurConflicts = curState.GetObjectConflict(Obj);
            double oCurCost = CostFunction(ObjCurConflicts, curState.TrialPositionIDs[index]);
            double NewCost =  this.State.State_Cost + (oNewCost - oCurCost);

            Obj.Translate((-1.0) * dx, (-1.0) * dy);//还原
            Obj.Translate(dx0, dy0);//还原
            return NewCost;
        }
        /// <summary>
        ///改进后的模拟退火算法
        /// （1）给定初温t0，随机产生初始状态s，令初始最优解s*=s，当前状态为s(0)=s，i=p=0；
         /// （2）令t=ti，以t，s*和s(i)调用改进的抽样过程，返回其所得最优解s*’和当前状态s’(k)，令当前状态s(i)=s’(k)；
         /// （3）判断C(s*)<C(s*’)? 若是，则令p=p+1；否则，令s*=s*’，p=0；
         /// （4）退温ti+1=update(ti)，令i=i+1；
         /// （5）判断p>m2? 若是，则转第(6)步；否则，返回第(2)步；
        /// （6）以最优解s*作为最终解输出，停止算法。
        /// </summary>
        public double DoSA(out int cn)
        {
            State curState = this.State;
            double a = this.Evaluate_InitState(curState);
            double A = a;
            cn = curState.Conflicts.Count;
            State bestState = curState.Clone() as State;
            int p = 0;
            double t = 0;
            if (a == 0)//或小于摸个阈值
                return 0;
            else
            {
                t = T0;
                while (p < N)
                {
                    State bestState1 = null;
                    this.DoSampling1(t, ref curState, out bestState1, M);
                    if (bestState1.State_Cost <bestState.State_Cost)
                    {
                        bestState = bestState1;
                        p = 0;
                    }
                    else
                    {
                        p++;//当最优状态趋于稳定后可终止
                    }
                    t = t * X;
                }
            }
            this.State = bestState;

            bestState.UpdateState(bestState, this.MapSDS, this.DispTemplate);
           
            return A;
        }

        /// <summary>
        /// 初始化状态并检查和评价冲突情况
        /// </summary>
        /// <param name="state">状态</param>
        /// <returns></returns>
        public double Evaluate_InitState(State state)
        {
            List<Conflict> conflictList = ConflictDetector.ConflictDetect(DTol);
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
        /// 产生小于oNo的随机整数
        /// </summary>
        /// <param name="fromNo">最小数</param>
        /// <param name="toNo">最大数</param>
        /// <returns>一个整数</returns>
        private int GenerateRandomNumber(int MaxNo)
        {
            Random random = new Random();
            return random.Next(MaxNo);
        }

        /// <summary>
        /// 产生一个新的状态
        /// </summary>
        /// <param name="state">原有状态</param>
        /// <returns>新的状态</returns>
        public State Generate_New_State(State state,out int  indexofObj,out SDS_PolygonO curObj,out int  indexofPos)
        {
            indexofObj = this.GenerateRandomNumber(this.MapSDS.PolygonOObjs.Count);
            curObj = MapSDS.PolygonOObjs[indexofObj];
            indexofPos = this.GenerateRandomNumber(this.DispTemplate.TrialPosiList.Count);
            State newState = state.Clone() as State;
            newState.TrialPositionIDs[indexofObj] = indexofPos;
            return newState;
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
                    cost += (this.PPCost * (this.DTol - conflict.Distance));
                }
                else if (conflict.ConflictType == @"PL")
                {
                    cost += (this.PLCost * (this.DTol - conflict.Distance));
                }
            }
            for (int i = 0; i < state.TrialPositionIDs.Length; i++)
            {
                if (state.TrialPositionIDs[i] != 0)
                {
                    cost += this.DispCost * DispTemplate.TrialPosiList[state.TrialPositionIDs[i]].Distance;
                }
            }
            return cost;
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
        /// 计算单个对象的状态值
        /// </summary>
        /// <returns>状态值</returns>
        private double CostFunction(List<Conflict> Conflicts, int PosIndex)
        {
            double cost = 0;
            if (Conflicts != null && Conflicts.Count > 0)
            {
                foreach (Conflict conflict in Conflicts)
                {
                    if (conflict.ConflictType == @"PP")
                    {
                        cost += (this.PPCost * (this.DTol - conflict.Distance));
                    }
                    else if (conflict.ConflictType == @"PL")
                    {
                        cost += (this.PLCost * (this.DTol - conflict.Distance));
                        //cost += (this.PLCost * conflict.Distance);

                    }
                }
            }

            if (PosIndex != 0)
            {
                double d = this.DispTemplate.TrialPosiList[PosIndex].Distance;
                cost += this.DispCost * d;
                //cost += this.DispCost * DispTemplate.TrialPosiList[PosIndex].Distance;
            }
            return cost;
        }
                /// <summary>
        /// 产生0-1之间的随机数
        /// </summary>
        /// <returns>0-1之间的随机数</returns>
        public double GenerateRandomFoat()
        {
            Random random = new Random();
            return random.NextDouble();
        }
    }
}
