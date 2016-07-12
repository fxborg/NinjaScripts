// Accelarated Moving Average v1.03     
// Copyright (C) 2016, fxborg<fxborg-labo.hateblo.jp>.
// http://fxborg-labo.hateblo.jp/ 
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;

#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// Accel Moving Average
    /// </summary>
    public class AccelMA : Indicator
    {
        private Series<double>        _mom;
        private Series<double>        _volat;
        private Series<double>        _ma;
        private List<double>          _accel;
        private int                   _accelPeriod;
        private double                _coef1,_coef2,_coef3;
        
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                //---パラメータのデフォルト値を設定。
                Description                 = "Accel Moving Average";
                Name                        = "AccelMA";
                IsSuspendedWhileInactive    = true;
                IsOverlay                   = true;
                AccelSpeed                  = 0.45;
                Period                      = 24;
                Smoothing                   = 10;

                AddPlot(Brushes.Orange, "Accel Moving Average");
            }
            else if (State == State.Configure)
            {
                //--- ここはセッション開始時に一度だけ実行される。
                _ma                         = new Series<double>(this);
                _mom                        = new Series<double>(this);
                _volat                      = new Series<double>(this);
                _accel                      = new List<double>();
                //--- アクセルフィルタ
                _accelPeriod=(int)(AccelSpeed * 15.0);
                for(int j=0;j<Period;j++) _accel.Add(Math.Pow(AccelSpeed,Math.Log(j+1)));
                //--- スムージングフィルタ
                double sq2= Math.Sqrt(2.0);
                double a = Math.Exp( -sq2 * Math.PI / Smoothing );
                _coef2 = 2.0 * a * Math.Cos(sq2 * Math.PI / Smoothing);
                _coef3 = -a * a;
                _coef1 = 1.0 - _coef2 - _coef3;
                //---
            }
        }
        
        protected override void OnBarUpdate()
        {
            if(CurrentBar<1)
              { //--- 1バーに満たない場合
                _mom[0]=0.0;
                _volat[0]=0.0;
                _ma[0]=Close[0];
                Main[0]=Close[0];
                return;
              }
            //--- 階差と階差の絶対値をセット
            _mom[0]=Close[0]-Close[1];
            _volat[0]=Math.Abs(Close[0]-Close[1]);

            if(CurrentBar<Math.Max(_accelPeriod,Period))
            {    //--- 計算期間に満たない場合
                _ma[0]=Close[0];                
                Main[0]=Close[0];
                return;
            }
            //--- 加速フィルタの計算
            double dsum=0.0000000001,volat=0.0000000001,dmax=0.0, dmin=0.0;
            for(int j=0;j<_accelPeriod;j++)
            {                 
                 dsum += _mom[j] *_accel[j];
                 if(dsum>dmax)dmax=dsum;
                 if(dsum<dmin)dmin=dsum;
            }
            //--- 期間内ボラティリティを計算
            for(int j=0;j<Period;j++) volat+=_volat[j];
            
            double range=Math.Max(0.0000000001,dmax-dmin);
            double accel1=range/volat;        
            //--- AccelMAを計算
            _ma[0]=accel1*(Close[0]-_ma[1])+_ma[1];              
            //--- さらにスムージング
            Main[0]= _coef1*_ma[0]+_coef2*Main[1]+_coef3*Main[2];
        }

        #region Properties
        //インジケーターバッファ
        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> Main
        {
            get { return Values[0]; }
        }        
        // パラメータ アクセルスピード 
        [Range(0.1, 0.9), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "AccelSpeed",
                                GroupName = "NinjaScriptParameters", Order = 1)]
        public double AccelSpeed
        { get; set; }

        // パラメータ 期間 
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Period", 
                                GroupName = "NinjaScriptParameters", Order = 2)]
        public int Period
        { get; set; }

        // パラメータ スムージング
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Smoothing",
                                GroupName = "NinjaScriptParameters", Order = 3)]
        public int Smoothing
        { get; set; }
        #endregion
    }
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private AccelMA[] cacheAccelMA;
        public AccelMA AccelMA(double accelSpeed, int period, int smoothing)
        {
            return AccelMA(Input, accelSpeed, period, smoothing);
        }

        public AccelMA AccelMA(ISeries<double> input, double accelSpeed, int period, int smoothing)
        {
            if (cacheAccelMA != null)
                for (int idx = 0; idx < cacheAccelMA.Length; idx++)
                    if (cacheAccelMA[idx] != null && cacheAccelMA[idx].AccelSpeed == accelSpeed && cacheAccelMA[idx].Period == period && cacheAccelMA[idx].Smoothing == smoothing && cacheAccelMA[idx].EqualsInput(input))
                        return cacheAccelMA[idx];
            return CacheIndicator<AccelMA>(new AccelMA(){ AccelSpeed = accelSpeed, Period = period, Smoothing = smoothing }, input, ref cacheAccelMA);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.AccelMA AccelMA(double accelSpeed, int period, int smoothing)
        {
            return indicator.AccelMA(Input, accelSpeed, period, smoothing);
        }

        public Indicators.AccelMA AccelMA(ISeries<double> input , double accelSpeed, int period, int smoothing)
        {
            return indicator.AccelMA(input, accelSpeed, period, smoothing);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.AccelMA AccelMA(double accelSpeed, int period, int smoothing)
        {
            return indicator.AccelMA(Input, accelSpeed, period, smoothing);
        }

        public Indicators.AccelMA AccelMA(ISeries<double> input , double accelSpeed, int period, int smoothing)
        {
            return indicator.AccelMA(input, accelSpeed, period, smoothing);
        }
    }
}

#endregion
