﻿using System;
using System.Collections.Generic;

namespace DataWrangler
{
    // Data container: Holds processed tick data and analytics
    public enum MktStateType { Summary = -1, Trade = 0, Ask = 1, Bid = 2 }
    
    public class MarketState
    {
        #region properties

        // Identifiers
        private readonly Security _securityObj;
        public string Name { get { return _securityObj.Name; } }
        public uint Id { get { return _securityObj.Id; } }

        // Read only properties
        public DateTime TimeStamp { get; private set; }
        public MktStateType StateType { get; private set; }
        public bool FirstOfInterval { get; private set; }

        // Updated on bid event
        public double Bid { get; private set; }
        public uint BidVol { get; private set; }
        public double PrevBid { get; private set; }
        public uint PrevBidVol { get; private set; }
        public int BidVolChg { get; private set; }
        public int BidVolChgSum { get; private set; }
        public int BidVolChgCnt { get; private set; }
        public double BidOpen { get; private set; }
        public uint BidVolOpen { get; private set; }

        // Updated on ask event
        public double Ask { get; private set; }
        public uint AskVol { get; private set; }
        public double PrevAsk { get; private set; }
        public uint PrevAskVol { get; private set; }
        public int AskVolChg { get; private set; }
        public int AskVolChgSum { get; private set; }
        public int AskVolChgCnt { get; private set; }
        public double AskOpen { get; private set; }
        public uint AskVolOpen { get; private set; }

        // Updated on quote event
        public double MidOpen { get; private set; }
        public double MidScaledOpen { get; private set; }
        public double Mid { get; private set; }
        public double MidScaled { get; private set; }

        // Updated on trade event
        public double LastTrdPrice { get; private set; }
        public uint LastTrdSize { get; private set; }
        public double LastPriceOpn { get; private set; }
        public double VolAtBid { get; private set; }
        public double TrdCntBid { get; private set; }
        public double VolAtAsk { get; private set; }
        public double TrdCntAsk { get; private set; }
        public double VolAtAskTdy { get; private set; }
        public double VolAtBidTdy { get; private set; }
        public double OrderFlowTdy { get; private set; }
        public double VolumeTdy { get; private set; }

        private uint _binCnt;
        public uint BinCnt
        {
            get { return _binCnt; }
            set { if (value > _binCnt) _binCnt = value; }
        }

        public SortedDictionary<double, TradesAtPrice> TrdsAtPrice = new SortedDictionary<double, TradesAtPrice>();

        public Dictionary<string, string> Codes;

        #endregion

        // constructor used on very first event to initialize market state
        public MarketState(Security security, TickData bid, TickData ask, TickData trade)
        {
            VolumeTdy = 0;
            OrderFlowTdy = 0;
            VolAtBidTdy = 0;
            VolAtAskTdy = 0;
            _securityObj = security;
            StateType = MktStateType.Summary;

            TimeStamp = bid.TimeStamp;
            FirstOfInterval = true;
            OnBidQuote(bid);
            SetBidOpen(bid);

            OnAskQuote(ask);
            SetAskOpen(ask);

            OnTrade(trade);
            SetTradeOpn(trade);

            SetMid();
            SetMidOpen();
        }

        // constructor used for each successive data event after the initial event
        public MarketState(Security security, MarketState previousMktState, TickData tickData) 
        {
            TimeStamp = DateTime.MinValue;
            VolumeTdy = 0;
            OrderFlowTdy = 0;
            VolAtBidTdy = 0;
            VolAtAskTdy = 0;
            _securityObj = security;

            if (previousMktState == null)
                throw new ArgumentException("Previous MarketState object must not be null", "previousMktState");

            TimeStamp = tickData.TimeStamp;
            FirstOfInterval = (tickData.TimeStamp.Subtract(previousMktState.TimeStamp).TotalSeconds != 0);

            CopyPrevState(previousMktState, FirstOfInterval);

            switch (tickData.Type)
            {
                case Type.Ask:
                    StateType = MktStateType.Ask;
                    OnAskQuote(tickData);
                    SetAskVolChg(previousMktState);
                    SetMid();
                    if (FirstOfInterval)
                    {
                        SetAskOpen(tickData);
                        SetMidOpen();
                    }
                    break;
                case Type.Bid:
                    StateType = MktStateType.Bid;
                    OnBidQuote(tickData);
                    SetBidVolChg(previousMktState);
                    SetMid();
                    if (FirstOfInterval)
                    {
                        SetBidOpen(tickData);
                        SetMidOpen();
                    }
                    break;
                case Type.Trade:
                    StateType = MktStateType.Trade;
                    OnTrade(tickData);
                    if (FirstOfInterval)
                    {
                        SetTradeOpn(tickData);
                    }
                    break;
                default:
                    throw new ArgumentException("TickData's 'Type' parameter must be of enum of type TickData.Type", "tickData");
            }

            if ((FirstOfInterval) || (Codes == null))
            {
                
                Codes = tickData.Codes;
            }
            else
            {
                if (tickData.Codes != null)
                {
                    if (tickData.Codes.Count > 0)
                    {
                        foreach (var code in tickData.Codes)
                        {
                            if (!Codes.ContainsKey(code.Key))
                                Codes.Add(code.Key, code.Value);
                        }
                    }
                }
            }
        }

        private void CopyPrevState(MarketState previous, bool isFirstOfInterval)
        {
            Bid = previous.Bid;
            BidVol = previous.BidVol;
            Ask = previous.Ask;
            AskVol = previous.AskVol;

            PrevBid = previous.PrevBid;
            PrevBidVol = previous.PrevBidVol;
            PrevAsk = previous.PrevAsk;
            PrevAskVol = previous.PrevAskVol;

            Mid = previous.Mid;
            MidScaled = previous.MidScaled;

            if (!isFirstOfInterval)
            {
                BidOpen = previous.BidOpen;
                BidVolOpen = previous.BidVolOpen;
                AskOpen = previous.AskOpen;
                AskVolOpen = previous.AskVolOpen;
                MidOpen = previous.MidOpen;
                MidScaledOpen = previous.MidScaledOpen;
                LastPriceOpn = previous.LastPriceOpn;

                VolAtBid = previous.VolAtBid;
                VolAtAsk = previous.VolAtAsk;
                TrdCntBid = previous.TrdCntBid;
                TrdCntAsk = previous.TrdCntAsk;
                BidVolChgSum = previous.BidVolChgSum;
                BidVolChgCnt = previous.BidVolChgCnt;
                AskVolChgSum = previous.AskVolChgSum;
                AskVolChgCnt = previous.AskVolChgCnt;
                TrdsAtPrice = previous.TrdsAtPrice;
            }
            else
            {
                BidOpen = previous.Bid;
                BidVolOpen = previous.BidVol;
                AskOpen = previous.Ask;
                AskVolOpen = previous.AskVol;
                MidOpen = previous.Mid;
                MidScaledOpen = previous.MidScaled;
                LastPriceOpn = previous.LastTrdPrice;
            }

        }

        private void OnBidQuote(TickData bidEvent)
        {
            PrevBid = Bid;
            Bid = bidEvent.Price;

            PrevBidVol = BidVol;
            BidVol = _securityObj.HasQuoteSize ? bidEvent.Size : 0; 
        }

        private void SetBidVolChg(MarketState prevMktState)
        {
            if (_securityObj.HasQuoteSize)
            {
                if ((Bid == prevMktState.Bid))
                {   
                    BidVolChg = (int)(BidVol - prevMktState.BidVol);
                    BidVolChgSum += BidVolChg;
                    SetBidVolChgCnt(BidVolChg);
                }
                else
                {
                    if ((Bid == prevMktState.Ask)) // just ticked up
                    {
                        BidVolChg = (int)(BidVol + prevMktState.AskVol);
                        BidVolChgSum += BidVolChg;
                        SetBidVolChgCnt(BidVolChg);
                        //Console.WriteLine(SecurityObj.Name + " went Bid @" + timeStamp.ToLongTimeString());
                    }
                    else
                    {
                        if ((Bid == prevMktState.PrevAsk)) // just ticked up, but need to look two data points back
                        {
                            BidVolChg = (int)(BidVol + prevMktState.PrevAskVol);
                            BidVolChgSum += BidVolChg;
                            SetBidVolChgCnt(BidVolChg);
                            //Console.WriteLine(SecurityObj.Name + " went Bid @" + timeStamp.ToLongTimeString());
                        }
                    }
                }
            }
        }

        private void SetBidVolChgCnt(int bidVolChg)
        {
            if (bidVolChg != 0 )
            {
                if (bidVolChg > 0)
                    BidVolChgCnt++;
                else
                    BidVolChgCnt--;
            }
        }

        private void SetBidOpen(TickData bidEvent)
        {
            BidOpen = bidEvent.Price;
            BidVolOpen = _securityObj.HasQuoteSize ? bidEvent.Size : 0;
        }

        private void OnAskQuote(TickData askEvent)
        {
            PrevAsk = Ask;
            Ask = askEvent.Price;

            PrevAskVol = AskVol;
            AskVol = _securityObj.HasQuoteSize? askEvent.Size : 0;  
        }

        private void SetAskVolChg(MarketState prevMktState)
        {
            if (_securityObj.HasQuoteSize)
            {
                if (Ask == prevMktState.Ask)
                {
                    AskVolChg = (int)(AskVol - prevMktState.AskVol);
                    AskVolChgSum += AskVolChg;
                    SetAskVolChgCnt(AskVolChg);
                }
                else
                {
                    if (Ask == prevMktState.Bid) // just ticked down
                    {
                        AskVolChg = (int)(AskVol + prevMktState.BidVol);
                        AskVolChgSum += AskVolChg;
                        SetAskVolChgCnt(AskVolChg);
                        //Console.WriteLine(SecurityObj.Name + " went offered @" + timeStamp.ToLongTimeString());
                    }
                    else
                    {
                        if (Ask == prevMktState.PrevBid) // just ticked down, but we need to look two data points back for price/volume
                        {
                            AskVolChg = (int)(AskVol + prevMktState.PrevBidVol);
                            AskVolChgSum += AskVolChg;
                            SetAskVolChgCnt(AskVolChg);
                            //Console.WriteLine(SecurityObj.Name + " went offered @" + timeStamp.ToLongTimeString());
                        }

                    }
                }
            }
        }

        private void SetAskVolChgCnt(int askVolChgCnt)
        {
            if (askVolChgCnt != 0)
            {
                if (askVolChgCnt > 0)
                    AskVolChgCnt++;
                else
                    AskVolChgCnt--;
            }
        }

        private void SetAskOpen(TickData askEvent)
        {
            AskOpen = askEvent.Price;
            AskVolOpen = _securityObj.HasQuoteSize ? askEvent.Size : 0;
        }

        private void OnTrade(TickData trade)
        {
            LastTrdPrice = trade.Price;
            if (LastTrdPrice == Bid) TrdCntBid++;
            if (LastTrdPrice == Ask) TrdCntAsk++;

            if ((_securityObj.HasTradeSize) && (trade.Size > 0))
            {
                LastTrdSize = trade.Size;
                if (LastTrdPrice == Bid) VolAtBid += LastTrdSize;
                if (LastTrdPrice == Ask) VolAtAsk += LastTrdSize;                
            }
            else
            {
                LastTrdSize = 0;
                VolAtBid = 0;
                VolAtAsk = 0;                
            }

            if (TrdsAtPrice.ContainsKey(trade.Price))
            {
                TrdsAtPrice[trade.Price].NewTradeAtPrice(LastTrdSize, this);
            }
            else
            {
                if ((_securityObj.HasTradeSize) && (trade.Size > 0))
                    TrdsAtPrice.Add(LastTrdPrice, new TradesAtPrice(LastTrdPrice, LastTrdSize, this));
                else
                    TrdsAtPrice.Add(LastTrdPrice, new TradesAtPrice(LastTrdPrice, this));
            }
        }
        
        private void SetTradeOpn(TickData trade)
        {
            LastPriceOpn = trade.Price;
        }

        private void SetMid()
        {
            Mid = (Bid + Ask) / 2;
            if ((_securityObj.HasQuoteSize) && (BidVol > 0) && (AskVol > 0))
            {
                MidScaled = (Ask - Bid) * BidVol / (BidVol + AskVol) + Bid;
            }
            else
            {
                MidScaled = Mid;
            }
        }
        
        private void SetMidOpen()
        {
            MidOpen = Mid;
            MidScaledOpen = MidScaled;
        }

        public string ToStringAllData()
        {
            string dataStr = _securityObj.Name +
                             " " + TimeStamp.ToLongTimeString() +
                             "  Type " + StateType.ToString() +
                             //"  Bid " + Bid.ToString() +
                             //"  BidOpn " + BidOpen.ToString() +
                             //"  BidVol " + BidVol.ToString() +
                             //"  BidVolOpen " + BidVolOpen.ToString() +
                             //"  BidVolChg " + BidVolChg.ToString() +
                             //"  BidVolChgSum " + BidVolChgSum.ToString() +
                             "  BidVolChgCnt " + BidVolChgCnt.ToString() +
                             //"  VolAtBid " + VolAtBid.ToString() +
                             //"  TrdCntBid " + TrdCntBid.ToString() +
                             //"  Ask " + Ask.ToString() +
                             //"  AskOpen " + AskOpen.ToString() +
                             //"  AskVol " + AskVol.ToString() +
                             //"  AskVolOpen " + AskVolOpen.ToString() +
                             //"  AskVolChg " + AskVolChg.ToString() +
                             //"  AskVolChgSum " + AskVolChgSum.ToString() +
                             "  AskVolChgCnt " + AskVolChgCnt.ToString() +
                             //"  VolAtAsk " + VolAtAsk.ToString() +
                             //"  TrdCntAsk  " + TrdCntAsk.ToString() +
                             "  Mid " + Mid.ToString() +
                             //"  MidOpn " + MidOpen.ToString() +
                             //"  MidScaled " + MidScaled.ToString("#.00") +
                             //"  MidScaledOpen " + MidScaledOpen.ToString("#.00") +
                             //"  LastPrice " + LastTrdPrice.ToString() +
                             //"  LastPriceOpn " + LastPriceOpn.ToString() +
                             "  LastSize " + LastTrdSize.ToString();

            // dataStr = SecurityObj.Name + Environment.NewLine + " {" + Environment.NewLine +
            //    "  LastSize " + LastTrdSize.ToString() + Environment.NewLine + " }";

            return dataStr;
        }

        public string GetHeadersString()
        {
            return "Name"
                   + ",DateTime"
                   + ",Type"
                   + ",Bid"
                   + ",BidOpn"
                   + ",BidVol"
                   + ",BidVolOpen"
                   + ",BidVolChg"
                   + ",BidVolChgSum"
                   + ",BidVolChgCnt"
                   + ",VolAtBid" 
                   + ",TrdCntBid" 
                   + ",Ask" 
                   + ",AskOpen" 
                   + ",AskVol"
                   + ",AskVolOpen"
                   + ",AskVolChg"
                   + ",AskVolChgSum"
                   + ",AskVolChgCnt"
                   + ",VolAtAsk"
                   + ",TrdCntAsk "
                   + ",Mid"
                   + ",MidOpn"
                   + ",MidScaled"
                   + ",MidScaledOpen"
                   + ",LastPrice"
                   + ",LastPriceOpn"
                   + ",LastSize";
        }

        public string ToFlatFileStringAllData()
        {
            const string del = ", ";
            string dataStr = _securityObj.Name +
                             del + TimeStamp.ToLongTimeString() +
                             del + StateType.ToString() +
                             del + Bid.ToString() +
                             del + BidOpen.ToString() +
                             del + BidVol.ToString() +
                             del + BidVolOpen.ToString() +
                             del + BidVolChg.ToString() +
                             del + BidVolChgSum.ToString() +
                             del + BidVolChgCnt.ToString() +
                             del + VolAtBid.ToString() +
                             del + TrdCntBid.ToString() +
                             del + Ask.ToString() +
                             del + AskOpen.ToString() +
                             del + AskVol.ToString() +
                             del + AskVolOpen.ToString() +
                             del + AskVolChg.ToString() +
                             del + AskVolChgSum.ToString() +
                             del + AskVolChgCnt.ToString() +
                             del + VolAtAsk.ToString() +
                             del + TrdCntAsk.ToString() +
                             del + Mid.ToString() +
                             del + MidOpen.ToString() +
                             del + MidScaled.ToString("#.0000") +
                             del + MidScaledOpen.ToString("#.0000") +
                             del + LastTrdPrice.ToString() +
                             del + LastPriceOpn.ToString() +
                             del + LastTrdSize.ToString();

            return dataStr;
        }
        
        public string ToStringAllTrades()
        {
            string output = _securityObj.Name +
                " " + TimeStamp.ToLongTimeString() + " " +
                ToStringAllTradesNoIndentity();

            return output.TrimEnd();
        }
        
        public string ToStringAllTradesNoIndentity()
        {
            string output = String.Empty;

            int prcCnt = 0;
            foreach (var p in TrdsAtPrice.Values)
            {
                string cnt = prcCnt.ToString() + " ";

                string priceStr = "Price" + cnt + p.Price.ToString() +
                    " Vol" + cnt + p.TotalVolume.ToString() +
                    " VolBid" + cnt + p.VolAtBid.ToString() +
                    " VolAsk" + cnt + p.VolAtAsk.ToString() +
                    " Cnt" + cnt + p.TradeCount.ToString() +
                    " CntBid" + cnt + p.CntAtBid.ToString() +
                    " CntAsk" + cnt + p.CntAtAsk.ToString();

                output += priceStr + " ";

                prcCnt++;
            }

            return output.TrimEnd();
        }

        public string ToFlatFileStringAllTrades(int maxSize)
        {
            const string del = ",";
            string output = del;

            int prcCnt = 0;
            foreach (var p in TrdsAtPrice.Values)
            {
                string priceStr = p.Price.ToString() +
                                  del + p.TotalVolume.ToString() +
                                  del + p.VolAtBid.ToString() +
                                  del + p.VolAtAsk.ToString() +
                                  del + p.TradeCount.ToString() +
                                  del + p.CntAtBid.ToString() +
                                  del + p.CntAtAsk.ToString();

                output += priceStr + ",";

                prcCnt++;
                if (prcCnt >= maxSize) break;
            }
             
            for (int i = prcCnt; i < maxSize; i++)
            {
                output += "0,0,0,0,0,0,0,0";
            }

            return output.TrimEnd();

        }

        public string GetTradesHeaderString(int maxSize)
        {

            const string del = ",";
            string output = ",";

            for (int i = 0; i < maxSize; i++)
            {
                string header = "Price" + i + del +
                                " Vol" + i + del +
                                " VolBid" + i + del +
                                " VolAsk" + i + del +
                                " Cnt" + i + del +
                                " CntBid" + i + del +
                                " CntAsk" + i + del;

                output += header;
            }

            return output;
        }

        public class TradesAtPrice
        {
            public double Price;

            public uint TotalVolume = 0;
            public uint VolAtBid = 0;
            public uint VolAtAsk = 0;

            public double TradeCount = 0;
            public uint CntAtBid = 0;
            public uint CntAtAsk = 0;

            public TradesAtPrice(double price, MarketState state)
            {
                Price = price;
                TradeCount++;

                if (price == state.Bid) CntAtBid++;
                else
                    if (price == state.Ask) CntAtAsk++;

            }

            public TradesAtPrice(double price, uint volume, MarketState state)
            {
                Price = price;
                TotalVolume = volume;
                TradeCount++;

                if (price == state.Bid)
                {
                    VolAtBid += volume;
                    CntAtBid++;
                }
                else
                {
                    if (price == state.Ask)
                    {
                        VolAtAsk += volume;
                        CntAtAsk++;
                    }
                }
            }

            public void NewTradeAtPrice(uint volume, MarketState state)
            {
                TotalVolume += volume;
                TradeCount++;

                if (Price == state.Bid)
                {
                    VolAtBid += volume;
                    CntAtBid++;
                }
                else
                {
                    if (Price == state.Ask)
                    {
                        VolAtAsk += volume;
                        CntAtAsk++;
                    }
                }
            }
        }
    }
}
