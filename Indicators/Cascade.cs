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
        private List<Series<double>>                    _lines;         // ステップライン用バッファ
        private List<Series<int>>                       _trends;        // トレンド用バッファ
        private Series<int>                             _trend;         // トレンド用バッファ統合
            
        private Series<double>                          _atr;           // ATR
        private int                                     _atrPeriod=100; // LasyATR用     
        private double                                  _atrAlpha;      // LasyATR用            

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Enter the description for your new custom Indicator here.";
                Name                                        = "Cascade";
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

                //See Help Guide for additional information.
                IsSuspendedWhileInactive                    = true;
                // インジケーターの出力設定
                AddPlot(new Stroke(Brushes.Blue),  PlotStyle.Bar,"Cascade"); // ライン
            
            }
            else if (State == State.Configure)
            {
                Plots[0].Width=10;          
                _atrAlpha=2.0/(_atrPeriod+1.0);

            }
            else if (State == State.Historical)
            {
                _atr            = new Series<double>(this);
                // Series<T>タイプのリスト
                _lines          = new List<Series<double>>();
                _trends         = new List<Series<int>>();
                _trend          = new Series<int>(this);
                // Series<T>を 好きなだけ追加出来る。
                for(int i=0;i<6;i++)    _lines.Add(new Series<double>(this));
                for(int i=0;i<6;i++)    _trends.Add(new Series<int>(this));
    
                
            }
        }

        protected override void OnBarUpdate()
        {
            //Add your custom indicator logic here.

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
            // Series<T>をループ内で扱える
            for(int i=0;i<6;i++)
            {
                //Series<T>を2次元配列っぽく使うことが出来ます。
                if(_trends[i][0]== 1)bull++;
                if(_trends[i][0]== -1)bear++;
            }

            if(CurrentBars[0]==0)return;
            // ここでデフォルトTF用のバッファにも書き込む
            //Hist[0]=1.0;
            double trend =(bull==6) ? 2.0 :
                      (bull==5) ? 1.0 :
                      (bear==6) ? -2.0 :
                      (bear==5) ? -1.0:
                      0.0;
            Trend[0]=trend;
            // トレンドによって線の色を変更
                if(trend==-2)       PlotBrushes[0][0] = Brushes.Maroon;
                else if(trend==-1)  PlotBrushes[0][0] = Brushes.Orange;
                else if(trend==0)   PlotBrushes[0][0] = Brushes.Yellow;
                else if(trend==1)   PlotBrushes[0][0] = Brushes.LimeGreen;
                else if(trend==2)   PlotBrushes[0][0] = Brushes.DarkGreen;
                        
            
            
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
        public Series<double> Trend
        {
            get { return Values[0]; }
        }
        
        // パラメータ ステップサイズ 
        [Range(0.01, 999.99), NinjaScriptProperty]
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
