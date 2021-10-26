using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    /// <summary>
    /// 符号描述类
    /// </summary>
    public  class Symbol
    {
        public int ID;//符号ID
        public int SylID;//符号编码
        public string LyrName;//图层名称
        public double Size;//符号大小
        public string FillColor;//填充颜色
        public string BorderColor;//边线颜色
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sid"></param>
        /// <param name="lyrName"></param>
        /// <param name="w"></param>
        /// <param name="fcolor"></param>
        /// <param name="bcolor"></param>
        public Symbol(int id,int sid,string lyrName,double s,string fcolor,string bcolor)
        {
            ID = id;
            SylID = sid;
            LyrName = lyrName;
            Size = s;
            FillColor = fcolor;
            BorderColor = bcolor;
        }
        /// <summary>
        /// 根据图层名查询符号
        /// </summary>
        /// <param name="lyrName"></param>
        /// <returns></returns>
        public static Symbol GetSymbolbyLyrName(string lyrName,List<Symbol> sylList)
        {
            foreach (Symbol s in sylList)
            {
                if (s.LyrName == lyrName)
                {
                    return s;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据符号编码查询符号
        /// </summary>
        /// <param name="lyrName"></param>
        /// <returns></returns>
        public static Symbol GetSymbolbysymID(int sylID, List<Symbol> sylList)
        {
            foreach (Symbol s in sylList)
            {
                if (s.SylID == sylID)
                {
                    return s;
                }
            }
            return null;
        }
    }
}
