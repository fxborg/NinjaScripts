#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Cascade : Indicator
    {
        #region private variables
        private int _setup=0;                                           // セットアップ判定用
                                                                        
        private List<Series<double>>                    _lines;　       // ステップライン用バッファ
        private List<Series<int>>                       _trends;　      // トレンド用バッファ
        private Series<int>                             _trend;         // トレンド用バッファ統合
            
        private Series<double>                          _atr;           // ATR
        private SMA                                     _ma;            // MA
        private Series<double>                          _price;         // Price
        private int                                     _atrPeriod=100; // LasyATR用     
        private double                                  _atrAlpha;      // LasyATR用            
        #endregion
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Cascade Indicator";
                Name                                        = "Cascade";
                Calculate                                   = Calculate.OnBarClose;
                IsOverlay                                   = true;
                DisplayInDataBox                            = true;
                DrawOnPricePanel                            = true;
                DrawHorizontalGridLines                     = true;
                DrawVerticalGridLines                       = true;
                PaintPriceMarkers                           = true;
                ScaleJustification                          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                // パラメータの初期値をセット
                StepSize=0.8;
                
                //See Help Guide for additional information.
                IsSuspendedWhileInactive                    = true;
                // インジケーターの出力設定
                AddPlot(Brushes.LightSteelBlue, "Cascade Line H"); // ライン
                AddPlot(Brushes.LightSteelBlue, "Cascade Line L"); // ライン
                AddPlot(new Stroke(Brushes.Red, 5), PlotStyle.TriangleDown, "Sell"); // 三角
                AddPlot(new Stroke(Brushes.DodgerBlue, 5), PlotStyle.TriangleUp, "Buy"); // 三角

                
                }
            else if (State == State.Configure)
            {
                // Series
                _price          = new Series<double>(this);
                _ma             = SMA(_price, 3);
                _atr            = new Series<double>(this);
                // Series<T>タイプのリスト
                _lines          = new List<Series<double>>();
                _trends         = new List<Series<int>>();
                _trend          = new Series<int>(this);
                // Series<T>を 好きなだけ追加出来る。
                for(int i=0;i<6;i++)    _lines.Add(new Series<double>(this));
                for(int i=0;i<6;i++)    _trends.Add(new Series<int>(this));
                
                
    
                _atrAlpha=2.0/(_atrPeriod+1.0);
    
            }
        }

        protected override void OnBarUpdate()
        {
            //Add your custom indicator logic here.
            _price[0]=(High[0]+Low[0]+Close[0])/3;
            // 最初のバー           
            if(CurrentBar==0){
                _atr[0]=High[0]-Low[0]; 
                return;
            }
            // LasyATRを計算
            calcLasyAtr();
            // StepLineを計算
            calcStepLines();
            //---
            int bull=0;
            int bear=0;
            double max=0;
            double min=double.MaxValue;
            // Series<T>をループ内で扱える
            for(int i=0;i<6;i++)
            {
                //Series<T>を2次元配列っぽく使うことが出来ます。
                if(_lines[i][0]>max)max=_lines[i][0];
                if(_lines[i][0]<min)min=_lines[i][0];
                if(_trends[i][0]== 1)bull++;
                if(_trends[i][0]== -1)bear++;
            }
            
            TrendH[0]=max;
            TrendL[0]=min;
            
            // トレンドによって背景色を変更
            if(bull==6)             Draw.Region(this,"Trend" + CurrentBar ,0,1,TrendH,TrendL,Brushes.DarkGreen,Brushes.DarkGreen,60);   
            else if(bull==5)        Draw.Region(this,"Trend" + CurrentBar ,0,1,TrendH,TrendL,Brushes.LimeGreen,Brushes.LimeGreen,60);   
            else if(bear==6)        Draw.Region(this,"Trend" + CurrentBar ,0,1,TrendH,TrendL,Brushes.Maroon,Brushes.Maroon,60); 
            else if(bear==5)        Draw.Region(this,"Trend" + CurrentBar ,0,1,TrendH,TrendL,Brushes.Red,Brushes.Orange,60);    
            else                    Draw.Region(this,"Trend" + CurrentBar ,0,1,TrendH,TrendL,Brushes.Yellow,Brushes.Yellow,60); 
            
            // トレンドを時系列データにセット
            _trend[0]=(bull==6) ? 2 :
                      (bull==5) ? 1 :
                      (bear==6) ? -2 :
                      (bear==5) ? -1 :
                      0;
            
            //--- ブレークアウト
            if(_trend[0]>=  1 && _trend[1]<  1) Buy[0]=Low[0]-_atr[0];
            if(_trend[0]<= -1 && _trend[1]> -1) Sell[0]=High[0]+_atr[0];

            //--- プルバック
            if(bull>=5) // 上昇トレンド
            {
                if(_setup==1)
                {
                    double low=Math.Min(Low[2],Low[1]);
                    if(Close[0]>Open[0] && Low[0]>low && signalBar(1)<High[0]) //シグナルバーを抜けた
                    {
                        _setup=0;
                        Buy[0]=Low[0]-_atr[0];
                    }   
                }
                else
                {
                    if(_ma[0]<_ma[1]) //押しのスタート
                    {
                        _setup=1;
                    }
                }
            }
            else if(bear>=5) //下降トレンド
            {
                if(_setup== -1)
                {
                    double high=Math.Max(High[1],High[2]);                  
                    if(Close[0]<Open[0] && High[0]<high && signalBar(-1)>Low[0]) // シグナルバーを抜けた
                    {
                        _setup=0;
                        Sell[0]=High[0]+_atr[0];
                    }   
                }
                else
                {
                    if(_ma[0]>_ma[1]) //戻しのスタート
                    {
                        _setup=-1;
                    }
                }
            }
            else
            {
                _setup=0;
            }
            
        }
        #region indicators
        //--- calc step lines
        private void calcStepLines()
        {
         
            double price=(Close[0]+High[0]+Low[0])/3.0;
            double rate=1.0;
            for(int i=0;i<6;i++)
            {
                double sz =_atr[0]*StepSize*rate;
                //--- 
                if((price-sz)>_lines[i][1]) _lines[i][0]=price-sz;
                else if((price+sz)<_lines[i][1]) _lines[i][0]=price+sz;
                else _lines[i][0]=_lines[i][1];
                //---
                if(_lines[i][0]>_lines[i][1]) _trends[i][0]=1;
                else if(_lines[i][0]<_lines[i][1])_trends[i][0]= -1;
                else _trends[i][0]=_trends[i][1];
                //---
                rate+=0.25;
            }  

        }
        //--- signal bar
        private double signalBar(int mode)
        {
            if(mode==1)
            {
                 double h12=Math.Abs(High[1]-High[2]);
                 double h23=Math.Abs(High[2]-High[3]);
                 double h13=Math.Abs(High[1]-High[3]);
                 double dmax;
                 if(High[2]>Math.Max(High[1],High[3]))              dmax=High[2];
                 else if (h12<Math.Min(h23,h13))                    dmax=Math.Max(High[1],High[2]);
                 else if (h13<Math.Min(h12,h23))                    dmax=Math.Max(High[1],High[3]);
                 else                                               dmax=Math.Max(High[2],High[3]);
                 return dmax;
                
            }
            else
            {
                
                 double l12=Math.Abs(Low[1]-Low[2]);
                 double l23=Math.Abs(Low[2]-Low[3]);
                 double l13=Math.Abs(Low[1]-Low[3]);
                 double dmin;
                 if(Low[2]<Math.Min(Low[1],Low[3]))                 dmin=Low[2];
                 else if (l12<Math.Min(l23,l13))                    dmin=Math.Min(Low[1],Low[2]);
                 else if (l13<Math.Min(l12,l23))                    dmin=Math.Min(Low[1],Low[3]);
                 else                                               dmin=Math.Min(Low[2],Low[3]);
                 return dmin;
            }
        }
        //--- calc Lasy ATR
        private void calcLasyAtr()
        {
          double tr = Math.Max(High[0],Close[1])-Math.Min(Low[0],Close[1]);
          tr=Math.Max(_atr[1]*0.667,Math.Min(tr,_atr[1]*1.333));
          _atr[0]=_atrAlpha*tr+(1.0-_atrAlpha)*_atr[1];
        }
        #endregion      
        #region Properties

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TrendH
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TrendL
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Sell
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Buy
        {
            get { return Values[3]; }
        }


        
        // パラメータ ステップサイズ 
        [Range(0.01, 9.99), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Step Size",
                                GroupName = "NinjaScriptParameters", Order = 1)]
        public double StepSize
        { get; set; }

        #endregion

        
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private Cascade[] cacheCascade;
        public Cascade Cascade(double stepSize)
        {
            return Cascade(Input, stepSize);
        }

        public Cascade Cascade(ISeries<double> input, double stepSize)
        {
            if (cacheCascade != null)
                for (int idx = 0; idx < cacheCascade.Length; idx++)
                    if (cacheCascade[idx] != null && cacheCascade[idx].StepSize == stepSize && cacheCascade[idx].EqualsInput(input))
                        return cacheCascade[idx];
            return CacheIndicator<Cascade>(new Cascade(){ StepSize = stepSize }, input, ref cacheCascade);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.Cascade Cascade(double stepSize)
        {
            return indicator.Cascade(Input, stepSize);
        }

        public Indicators.Cascade Cascade(ISeries<double> input , double stepSize)
        {
            return indicator.Cascade(input, stepSize);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.Cascade Cascade(double stepSize)
        {
            return indicator.Cascade(Input, stepSize);
        }

        public Indicators.Cascade Cascade(ISeries<double> input , double stepSize)
        {
            return indicator.Cascade(input, stepSize);
        }
    }
}

#endregion
