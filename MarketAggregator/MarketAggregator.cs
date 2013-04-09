﻿using System;
using System.Collections.Generic;

namespace DataWrangler
{
    public class MarketAggregator
    {
        public enum Mode { RealTime = 1, Historical = 0 }
        public Mode InputMode { get; set; }

        public enum OutPutMode { FlatFile, Xml, Binary, SqlTable }
        public OutPutMode ExportMode { get; set; }

        public string OutputPath { get; set; }

        // main data repository
        public SortedDictionary<DateTime, Dictionary<Security, SortedDictionary<uint, MarketState>>>
            Markets = new SortedDictionary<DateTime, Dictionary<Security, SortedDictionary<uint, MarketState>>>();

        private DateTime _lastState = DateTime.MinValue;

        private readonly List<DataFactory> _securitites = new List<DataFactory>();

        public MarketAggregator()
        {
            InputMode = Mode.RealTime;
        }

        public void AddSecurity(DataFactory factory)
        {
            _securitites.Add(factory);
        }

        public void AddTickData(DataFactory factory, SortedDictionary<uint, MarketState> state, DateTime stateTime)
        {

            if (!Markets.ContainsKey(stateTime))
            {
                Markets.Add(stateTime, new Dictionary<Security, SortedDictionary<uint, MarketState>>());
            }

            lock (Markets[stateTime])
            {
                Dictionary<Security, SortedDictionary<uint, MarketState>> allMarketsAtTime = Markets[stateTime];


                foreach (DataFactory f in _securitites)
                {
                    // no market data for this security, for this time stamp exists
                    if (!allMarketsAtTime.ContainsKey(f.SecurityObj))
                    {
                        SortedDictionary<uint, MarketState> mktData = factory.Equals(f) ? state : f.GetCurrentOrBefore(stateTime);
                        allMarketsAtTime.Add(f.SecurityObj, mktData);
                    }
                    else // market data for this security, for this time stamp exists already
                    {
                        if (factory.Equals(f))
                        {
                            allMarketsAtTime[f.SecurityObj] = state;
                        }
                    }
                }

                if (_lastState < stateTime) _lastState = stateTime;

            }
            // check if DateTime stamp exists.
            //  If not, create it then
            //  create a security entry for each security and fill it with last seconds state

            
            // if exsits, replace with latest data
        }

        public void BatchWriteOutData(OutPutMode outPutMode)
        {
            switch (outPutMode)
            {
                case OutPutMode.FlatFile:
                    WriteOutFlatFile();
                    break;
                case OutPutMode.Xml:
                    break;
                case OutPutMode.Binary:
                    break;
                case OutPutMode.SqlTable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("outPutMode");
            }
        }

        private void WriteOutFlatFile()
        {
            bool headerWritten = false;
            foreach (var second in Markets)
            {
                foreach (var security in second.Value)
                {
                    MarketState lastTick = security.Value[(uint) (security.Value.Count - 1)];

                    if (!headerWritten)
                    {
                        Console.WriteLine(lastTick.GetHeadersString() + lastTick.GetTradesHeaderString(3));
                        headerWritten = true;
                    }

                    string lastTickStr = MarketStateToString(lastTick) + ",";
                    Console.WriteLine(lastTickStr);
                }
            }
        }

        private string MarketStateToString(MarketState lastTick)
        {
            string output = lastTick.ToFlatFileStringAllData() + lastTick.ToFlatFileStringAllTrades(3);

            return output;
        }


    }
}
