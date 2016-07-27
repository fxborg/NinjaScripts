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
    public class CascadeMTF : Indicator
    {
        private List<Series<double>>                    _lines;         // ステップライン用バッファ
        private List<Series<int>>                       _trends;        // トレンド用バッファ
        private Series<int>                             _trend;         // トレンド用バッファ統合
            
        private Series<double>                          _atr;           // ATR
        private int                                     _atrPeriod=100; // LasyATR用     
        private double                                  _atrAlpha;      // LasyATR用            
        private double                                  _exposedVariable;
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Cascade MTF";
                Name                                        = "CascadeMTF";
                Calculate                                   = Calculate.OnBarClose;
                IsOverlay                                   = false;
                DisplayInDataBox                            = true;
                DrawOnPricePanel                            = true;
                DrawHorizontalGridLines                     = true;
                DrawVerticalGridLines                       = true;
                PaintPriceMarkers                           = true;
                ScaleJustification                          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                // パラメータの初期値をセット
                StepSize=0.8;
                TF=60;
                //See Help Guide for additional information. 
                IsSuspendedWhileInactive                    = true;
                // インジケーターの出力設定
                AddPlot(new Stroke(Brushes.Blue),  PlotStyle.Bar,"CascadeMTF LINE"); // ライン
            }
            else if (State == State.Configure)
            {
                //指定されたタイムフレームのバーオブジェクトを追加します。
                AddDataSeries(BarsPeriodType.Minute, TF);
                
                _atrAlpha=2.0/(_atrPeriod+1.0);
                Plots[0].Width=5;           

            } 
            else if (State == State.Historical)     //ヒストリカルデータの処理中、リアルタイムでは１度だけバックテストの最適化時は１最適化毎に呼ばれます。      
            {   
                //デフォルトタイムフレーム用のバッファ、オーナーをthisにします。 
                _trend          = new Series<int>(this);

                //MTF用のバッファ、オーナーに BarsArray[1] を指定します。
                _atr            = new Series<double>(BarsArray[1]);

                // Series<T>タイプのリスト
                _lines          = new List<Series<double>>();
                _trends         = new List<Series<int>>();
                
                // Series<T>を 好きなだけ追加出来る。
                for(int i=0;i<6;i++)    _lines.Add(new Series<double>(BarsArray[1]));
                for(int i=0;i<6;i++)    _trends.Add(new Series<int>(BarsArray[1]));
    

            }
        }

        protected override void OnBarUpdate()
        {
            
            //Add your custom indicator logic here.
            if (BarsInProgress == 1) //BarsArray[1]の更新時
            {
              
                // 最初のバー           
                if(CurrentBars[1]==0){
                    _atr[0]=Highs[1][0]-Lows[1][0]; 
                    return;
                }

                // LasyATRを計算
                double tr = Math.Max(Highs[1][0],Closes[1][1])-Math.Min(Lows[1][0],Closes[1][1]);
                tr=Math.Max(_atr[1]*0.667,Math.Min(tr,_atr[1]*1.333));
                _atr[0]=_atrAlpha*tr+(1.0-_atrAlpha)*_atr[1];           
                // StepLineを計算
                calcStepLines();
                //---
                int bull=0;
                int bear=0;
                double sum=0.0;
                // Series<T>をループ内で扱える
                for(int i=0;i<6;i++)
                {
                    //Series<T>を2次元配列っぽく使うことが出来ます。
                    sum+=_lines[i][0];
                    if(_trends[i][0]== 1)bull++;
                    if(_trends[i][0]== -1)bear++;
                }
                if(CurrentBars[0]==0)return;
                // ここでデフォルトTF用のバッファにも書き込む
                Line[0]=1.0;
                _trend[0]=(bull==6) ? 5 :
                          (bull==5) ? 4 :
                          (bear==6) ? 1 :
                          (bear==5) ? 2 :
                          3;
                
            }   
            if (BarsInProgress == 0)// BarsArray[0](デフォルトTF)の更新時
            {
                if(CurrentBars[0]>0 )  
                {
                    // 上位足の結果をコピーする。
                    Line[0] = Line[1];
                    _trend[0] = _trend[1];
                    
                    // トレンドによって線の色を変更
                    if(_trend[0]==1)        PlotBrushes[0][0] = Brushes.Maroon;
                    else if(_trend[0]==2)   PlotBrushes[0][0] = Brushes.Orange;
                    else if(_trend[0]==3)   PlotBrushes[0][0] = Brushes.Yellow;
                    else if(_trend[0]==4)   PlotBrushes[0][0] = Brushes.LimeGreen;
                    else if(_trend[0]==5)   PlotBrushes[0][0] = Brushes.DarkGreen;
                }   
                // おまじない
                _exposedVariable = Close[0];
            }
        }   
        #region indicators
        //--- calc step lines
        private void calcStepLines()
        {
         
            double price=Typical[0];
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

        #endregion

        
        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Line
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> Trend
        {
            get { return _trend; }
        }

        // タイムフレーム
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "TimeFrame",
                                GroupName = "NinjaScriptParameters", Order = 0)]
        public int TF
        { get; set; }

        public double ExposedVariable
        {
            // We need to call the Update() method to ensure our exposed variable is in up-to-date.
            get { Update(); return _exposedVariable; }
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
        private CascadeMTF[] cacheCascadeMTF;
        public CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return CascadeMTF(Input, tF, stepSize);
        }

        public CascadeMTF CascadeMTF(ISeries<double> input, int tF, double stepSize)
        {
            if (cacheCascadeMTF != null)
                for (int idx = 0; idx < cacheCascadeMTF.Length; idx++)
                    if (cacheCascadeMTF[idx] != null && cacheCascadeMTF[idx].TF == tF && cacheCascadeMTF[idx].StepSize == stepSize && cacheCascadeMTF[idx].EqualsInput(input))
                        return cacheCascadeMTF[idx];
            return CacheIndicator<CascadeMTF>(new CascadeMTF(){ TF = tF, StepSize = stepSize }, input, ref cacheCascadeMTF);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return indicator.CascadeMTF(Input, tF, stepSize);
        }

        public Indicators.CascadeMTF CascadeMTF(ISeries<double> input , int tF, double stepSize)
        {
            return indicator.CascadeMTF(input, tF, stepSize);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return indicator.CascadeMTF(Input, tF, stepSize);
        }

        public Indicators.CascadeMTF CascadeMTF(ISeries<double> input , int tF, double stepSize)
        {
            return indicator.CascadeMTF(input, tF, stepSize);
        }
    }
}

#endregion
