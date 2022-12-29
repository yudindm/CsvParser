using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CsvParser
{
    class CsvParser
    {
        private enum ParseState { LineStart, FldStart, QuotedData, UnquotedData, FldEnd }
        private TextReader _rdr;
        private StringBuilder _sb = new StringBuilder();
        private List<int> _lengths = new List<int>();
        private int _forward = -1;
        public CsvParser(TextReader rdr)
        {
            _rdr = rdr;
        }
        public string[] NextLine()
        {
            _lengths.Clear();
            _sb.Clear();
            var state = ParseState.LineStart;
            var fldLen = 0;
            while (true)
            {
                var ch = NextChar();
                if (ch == -1)
                {
                    if (state == ParseState.QuotedData)
                        throw new InvalidDataException("Unexpected end of stream.");
                    if (state == ParseState.LineStart)
                        return null;
                    _lengths.Add(fldLen);
                    break;
                }
                else if (ch == '\n' || ch == '\r')
                {
                    if (state != ParseState.QuotedData)
                    {
                        _forward = NextChar();
                        if (_forward == '\n' || _forward == '\r')
                            _forward = -1;
                        _lengths.Add(fldLen);
                        break;
                    }
                }
                else if (ch == ',')
                {
                    if (state != ParseState.QuotedData)
                    {
                        _lengths.Add(fldLen);
                        fldLen = 0;
                        state = ParseState.FldStart;
                        continue;
                    }
                }
                else if (ch == '"')
                {
                    if (state == ParseState.QuotedData)
                    {
                        _forward = NextChar();
                        if (_forward == '"')
                            _forward = -1;
                        else if (_forward == ',' || _forward == '\n' || _forward == '\r' || _forward == -1)
                        {
                            state = ParseState.FldEnd;
                            continue;
                        }
                    }
                    else if (state == ParseState.FldStart || state == ParseState.LineStart)
                    {
                        state = ParseState.QuotedData;
                        continue;
                    }
                }
                else if (ch == ' ')
                {
                    if (state == ParseState.FldStart || state == ParseState.LineStart || state == ParseState.FldEnd)
                        continue;
                }

                if (state == ParseState.FldEnd)
                    throw new InvalidDataException("Unexpected char after the quoted field end.");

                _sb.Append((char)ch);
                fldLen++;
            }
            return PackResult();
        }
        private int NextChar()
        {
            if (_forward == -1)
                return _rdr.Read();
            else
            {
                var result = _forward;
                _forward = -1;
                return result;
            }
        }
        private string[] PackResult()
        {
            var result = new string[_lengths.Count];
            var p = 0;
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = _sb.ToString(p, _lengths[i]);
                p += _lengths[i];
            }
            return result;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists(".\\PerfEOS_IISTrace.csv"))
            {
                Console.WriteLine("Unpack PerfEOS_IISTrace.csv from PerfEOS_IISTrace.7z first, than run program again.");
                return;
            }
            using (var rdr = new StreamReader(".\\PerfEOS_IISTrace.csv")) //test.csv
            {
                var cnt = 0;
                try
                {
                    var start = DateTime.Now;
                    var prsr = new CsvParser(rdr);
                    while (true)
                    {
                        var values = prsr.NextLine();
                        if (values == null)
                            break;
                        cnt++;
                        if ((cnt % 10000) == 0)
                            Console.WriteLine("{0}   {1}", cnt, DateTime.Now - start);
                    }
                    Console.WriteLine("{0}   {1}", cnt, DateTime.Now - start);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(cnt);
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
