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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class CascadeTrend2 : Strategy
    {
        #region private variables
        private SMA                                     _ma1;           //MA1       
        private SMA                                     _ma2;           //MA2       
        private Cascade                                 _cascade;
        #endregion
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Cascade Trend Entry.";
                Name                                        = "CascadeTrend2";
                Calculate                                   = Calculate.OnBarClose;
                EntriesPerDirection                         = 1;
                EntryHandling                               = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy                = true;
                ExitOnSessionCloseSeconds                   = 30;
                IsFillLimitOnTouch                          = false;
                MaximumBarsLookBack                         = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution                         = OrderFillResolution.Standard;
                Slippage                                    = 0;
                StartBehavior                               = StartBehavior.WaitUntilFlat;
                TimeInForce                                 = TimeInForce.Gtc;
                TraceOrders                                 = false;
                RealtimeErrorHandling                       = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling                          = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade                         = 10;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration   = true;

                // パラメータの初期値をセット
                StepSize=1.0;
                MaPeriod=800;
        
            
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.Historical)
            {
                // Series
                _ma1            = SMA(BarsArray[0], MaPeriod);
                _ma2            = SMA(BarsArray[0], MaPeriod*5);
                _cascade        = Cascade(BarsArray[0],StepSize);
                
            }
        }

        protected override void OnBarUpdate()
        {
            //Add your custom indicator logic here.
            if(CurrentBars[0]<=2)     return;
                
                
            int trend0 = (int)_cascade.Trend[0];
            int trend1 = (int)_cascade.Trend[1];
            //--- Exit Position
            if(MarketPosition.Long == Position.MarketPosition)
            {
                if(trend0<1 )ExitLong();
            }
            if(MarketPosition.Short== Position.MarketPosition)
            {
                if(trend0>-1)ExitShort();
            }
            //---
            if(trend0 >=  2 && trend1 <  2 && _ma1[0]>_ma2[0])             //上昇トレンド転換時            
            {   
                EnterLong();
            }
            else if(trend0 <= -2 && trend1> -2 && _ma1[0]<_ma2[0])        //下降トレンド転換時
            {
                EnterShort();               
            }
            
            //---           
        }
        #region indicators
        //--- signal bar
        private double signalBar(int mode)
        {
            if(mode==1)
            {
                 double h12=Math.Abs(High[0]-High[1]);
                 double h23=Math.Abs(High[1]-High[2]);
                 double h13=Math.Abs(High[0]-High[2]);
                 double dmax;
                 if(High[1]>Math.Max(High[0],High[2]))              dmax=High[1];
                 else if (h12<Math.Min(h23,h13))                    dmax=Math.Max(High[0],High[1]);
                 else if (h13<Math.Min(h12,h23))                    dmax=Math.Max(High[0],High[2]);
                 else                                               dmax=Math.Max(High[1],High[2]);
                 return dmax;
                
            }
            else
            {
                
                 double l12=Math.Abs(Low[0]-Low[1]);
                 double l23=Math.Abs(Low[1]-Low[2]);
                 double l13=Math.Abs(Low[0]-Low[2]);
                 double dmin;
                 if(Low[1]<Math.Min(Low[0],Low[2]))                 dmin=Low[1];
                 else if (l12<Math.Min(l23,l13))                    dmin=Math.Min(Low[0],Low[1]);
                 else if (l13<Math.Min(l12,l23))                    dmin=Math.Min(Low[0],Low[2]);
                 else                                               dmin=Math.Min(Low[1],Low[2]);
                 return dmin;
            }
        }
        #endregion      

        #region Properties

        // パラメータ ステップサイズ 
        [Range(0.01, double.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Step Size",
                                GroupName = "NinjaScriptParameters", Order = 0)]
        public double StepSize
        { get; set; }

                
        // MA period
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "MA Period",
                                GroupName = "NinjaScriptParameters", Order = 3)]
        public int MaPeriod
        { get; set; }

        
        #endregion
    }
}