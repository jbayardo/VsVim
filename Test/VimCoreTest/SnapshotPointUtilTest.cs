﻿using System;
using System.Linq;
using Vim.EditorHost;
using Microsoft.VisualStudio.Text;
using Vim.Extensions;
using Xunit;

namespace Vim.UnitTest
{
    public class SnapshotPointUtilTest : VimTestBase
    {
        private static readonly string[] s_lines = new string[]
            {
                "summary description for this line",
                "some other line",
                "running out of things to make up"
            };

        private ITextBuffer _textBuffer = null;
        private ITextSnapshot _snapshot = null;

        internal void Create(params string[] lines)
        {
            _textBuffer = CreateTextBuffer(lines);
            _snapshot = _textBuffer.CurrentSnapshot;
        }

        [WpfFact]
        public void GetLineRangeSpan1()
        {
            Create(s_lines);
            var span = SnapshotPointUtil.GetLineRangeSpan(new SnapshotPoint(_snapshot, 0), 1);
            var line = _snapshot.GetLineFromLineNumber(0);
            Assert.Equal(line.Extent, span);
        }

        /// <summary>
        /// Multi-line range
        /// </summary>
        [WpfFact]
        public void GetLineRangeSpan2()
        {
            Create(s_lines);
            var span = SnapshotPointUtil.GetLineRangeSpan(new SnapshotPoint(_snapshot, 0), 2);
            var start = _snapshot.GetLineFromLineNumber(0);
            var second = _snapshot.GetLineFromLineNumber(1);
            var expected = new Span(start.Start, second.End - start.Start);
            Assert.Equal(span.Span, expected);
        }

        [WpfFact]
        public void GetLineRangeSpanIncludingLineBreak1()
        {
            Create("foo", "bar");
            var span = SnapshotPointUtil.GetLineRangeSpanIncludingLineBreak(new SnapshotPoint(_snapshot, 0), 1);
            Assert.Equal(_snapshot.GetLineFromLineNumber(0).ExtentIncludingLineBreak, span);
        }

        [WpfFact]
        public void GetCharacterSpan1()
        {
            Create("foo");
            var span = SnapshotPointUtil.GetCharacterSpan(new SnapshotPoint(_textBuffer.CurrentSnapshot, 0));
            Assert.Equal(0, span.Start.Position);
            Assert.Equal(1, span.Length);
        }

        /// <summary>
        /// Empty line shtould have a character span of the entire line
        /// </summary>
        [WpfFact]
        public void GetCharacterSpan2()
        {
            Create("foo", string.Empty, "baz");
            var line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(1);
            var span = SnapshotPointUtil.GetCharacterSpan(line.Start);
            Assert.Equal(span, line.ExtentIncludingLineBreak);
        }

        /// <summary>
        /// End of line should have the span of the line break
        /// </summary>
        [WpfFact]
        public void GetCharacterSpan3()
        {
            Create("foo", "bar");
            var line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var span = SnapshotPointUtil.GetCharacterSpan(line.End);
            Assert.Equal(span, new SnapshotSpan(line.End, line.EndIncludingLineBreak));
        }

        [WpfFact]
        public void GetNextPointWithWrap1()
        {
            Create("foo", "baz");
            var line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPointWithWrap(line.Start);
            Assert.Equal(1, next.Position);
        }

        /// <summary>
        /// End of line should wrap
        /// </summary>
        [WpfFact]
        public void GetNextPointWithWrap2()
        {
            Create("foo", "bar");
            var line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPointWithWrap(line.End);
            line = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(1);
            Assert.Equal(line.Start, next);
        }

        /// <summary>
        /// Wrap around the buffer
        /// </summary>
        [WpfFact]
        public void GetNextPointWithWrap3()
        {
            Create("foo", "bar");
            var next = SnapshotPointUtil.GetNextPointWithWrap(_textBuffer.CurrentSnapshot.GetLineFromLineNumber(1).End);
            Assert.Equal(_textBuffer.CurrentSnapshot.GetLineFromLineNumber(0).Start, next);
        }

        [WpfFact]
        public void GetPreviousPointWithWrap1()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(new SnapshotPoint(_textBuffer.CurrentSnapshot, 1));
            Assert.Equal(_textBuffer.CurrentSnapshot.GetLineFromLineNumber(0).Start, prev);
        }

        [WpfFact]
        public void GetPreviousPointWithWrap2()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(_textBuffer.CurrentSnapshot.GetLineFromLineNumber(1).Start);
            Assert.Equal(_textBuffer.CurrentSnapshot.GetLineFromLineNumber(0).End, prev);
        }

        [WpfFact]
        public void GetPreviousPointWithWrap3()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(new SnapshotPoint(_textBuffer.CurrentSnapshot, 0));
            Assert.Equal(SnapshotUtil.GetEndPoint(_textBuffer.CurrentSnapshot), prev);
        }

        [WpfFact]
        public void GetLines1()
        {
            Create("foo", "bar");
            var point = new SnapshotPoint(_snapshot, 0);
            var agg = SnapshotPointUtil.GetLines(point, SearchPath.Forward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.Equal("foobar", agg);
        }

        /// <summary>
        /// Check forward wraping
        /// </summary>
        [WpfFact]
        public void GetLines2()
        {
            Create("foo", "bar", "baz");
            var point = new SnapshotPoint(_snapshot, 6);
            var agg = SnapshotPointUtil.GetLines(point, SearchPath.Forward)
                .Select(x => x.GetText())
                .Aggregate((x, y) => x + y);
            Assert.Equal("barbaz", agg);
        }

        [WpfFact]
        public void GetLines3()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetLines(line.Start.Subtract(1), SearchPath.Backward);
            Assert.Single(list);
        }

        [WpfFact]
        public void GetLines4()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchPath.Backward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.Equal("cba", msg);
        }

        [WpfFact]
        public void GetLines5()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchPath.Forward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.Equal("cde", msg);
        }

        /// <summary>
        /// If going forward and starting from the end don't return any spans
        /// </summary>
        [WpfFact]
        public void GetSpans_FromEnd()
        {
            Create("foo");
            var line = _snapshot.GetLineFromLineNumber(0);
            var list = SnapshotPointUtil.GetSpans(SearchPath.Forward, line.End);
            Assert.Empty(list);
        }

        /// <summary>
        /// Don't wrap backwards if we don't say wrap
        /// </summary>
        [WpfFact]
        public void GetSpans5()
        {
            Create("foo");
            var line = _snapshot.GetLineFromLineNumber(0);
            var list = SnapshotPointUtil.GetSpans(SearchPath.Backward, line.Start + 2);
            Assert.Single(list);
        }

        /// <summary>
        /// Multi lack of wrap
        /// </summary>
        [WpfFact]
        public void GetSpans6()
        {
            Create("foo", "bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(SearchPath.Forward, line.Start + 1);
            Assert.Equal(2, list.Count());
        }

        /// <summary>
        /// Don't include the provided point when getting spans backward
        /// </summary>
        [WpfFact]
        public void GetSpans_DontIncludePointGoingBackward()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(SearchPath.Backward, line.Start).Select(x => x.GetText()).ToList();
            Assert.Equal(new[] { "foo bar" }, list);
        }

        [WpfFact]
        public void GetSpans8()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(SearchPath.Backward, line.Start.Subtract(1));
            Assert.Single(list);
        }

        [WpfFact]
        public void GetCharOrDefault1()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).Start;
            Assert.Equal('f', SnapshotPointUtil.GetCharOrDefault(point, 'g'));
        }

        [WpfFact]
        public void GetCharOrDefault2()
        {
            Create("foo", "bar");
            var endPoint = new SnapshotPoint(_textBuffer.CurrentSnapshot, _textBuffer.CurrentSnapshot.Length);
            var didSee = false;
            try
            {
                var notUsed = endPoint.GetChar();
            }
            catch (ArgumentException)
            {
                didSee = true;
            }
            Assert.True(didSee);
            Assert.Equal('f', SnapshotPointUtil.GetCharOrDefault(endPoint, 'f'));
        }

        /// <summary>
        /// All points should be valid
        /// </summary>
        [WpfFact]
        public void GetPoints1()
        {
            Create("foo", "bar");
            var start = SnapshotUtil.GetStartPoint(_textBuffer.CurrentSnapshot);
            foreach (var cur in SnapshotPointUtil.GetPoints(SearchPath.Forward, start))
            {
                var notUsed = cur.GetChar();
            }
        }

        [WpfFact]
        public void GetPoints2()
        {
            Create("foo bar");
            var start = SnapshotUtil.GetStartPoint(_textBuffer.CurrentSnapshot).Add(1);
            var first = SnapshotPointUtil.GetPoints(SearchPath.Forward, start).First();
            Assert.Equal('o', first.GetChar());
        }

        [WpfFact]
        public void GetPoints3()
        {
            Create("foo bar");
            var start = SnapshotUtil.GetStartPoint(_textBuffer.CurrentSnapshot);
            var points = SnapshotPointUtil.GetPoints(SearchPath.Forward, start);
            var str = points.Select(x => x.GetChar().ToString()).Aggregate((x, y) => x + y);
            Assert.Equal("foo bar", str);
        }

        /// <summary>
        /// All points should be valid
        /// </summary>
        [WpfFact]
        public void GetPoints4()
        {
            Create("foo", "bar");
            var start = SnapshotUtil.GetStartPoint(_textBuffer.CurrentSnapshot);
            foreach (var cur in SnapshotPointUtil.GetPoints(SearchPath.Forward, start))
            {
                var notUsed = cur.GetChar();
            }
        }

        [WpfFact]
        public void GetPoints5()
        {
            Create("foo bar");
            var start = SnapshotUtil.GetStartPoint(_textBuffer.CurrentSnapshot).Add(1);
            var first = SnapshotPointUtil.GetPoints(SearchPath.Backward, start).First();
            Assert.Equal('o', first.GetChar());
        }

        [WpfFact]
        public void GetPoints6()
        {
            Create("foo bar");
            var start = _textBuffer.GetEndPoint();
            var points = SnapshotPointUtil.GetPoints(SearchPath.Backward, start);
            var str = points.Select(x => x.GetChar().ToString()).Aggregate((x, y) => x + y);
            Assert.Equal("rab oof", str);
        }

        [WpfFact]
        public void GetPoints7()
        {
            Create("foo bar");
            var start = _textBuffer.CurrentSnapshot.GetLineRange(0).End;
            var points = SnapshotPointUtil.GetPoints(SearchPath.Backward, start);
            var str = points.Select(x => x.GetChar().ToString()).Aggregate((x, y) => x + y);
            Assert.Equal("rab oof", str);
        }

        [WpfFact]
        public void TryGetNextPointOnLine1()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).Start;
            var res = SnapshotPointUtil.TryGetNextPointOnLine(point, 1);
            Assert.True(res.IsSome());
            Assert.Equal(point.Add(1), res.Value);
        }

        [WpfFact]
        public void TryGetNextPointOnLine2()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).End;
            var res = SnapshotPointUtil.TryGetNextPointOnLine(point, 1);
            Assert.False(res.IsSome());
        }

        [WpfFact]
        public void TryGetNextPointOnLine3()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).Start.Add(1);
            var res = SnapshotPointUtil.TryGetNextPointOnLine(point, 1);
            Assert.True(res.IsSome());
            Assert.Equal(point.Add(1), res.Value);
        }

        [WpfFact]
        public void TryGetPreviousPointOnLine1()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).End.Subtract(1);
            var res = SnapshotPointUtil.TryGetPreviousPointOnLine(point, 1);
            Assert.True(res.IsSome());
            Assert.Equal(point.Subtract(1), res.Value);
        }

        [WpfFact]
        public void TryGetPreviousPointOnLine2()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).Start.Add(1);
            var res = SnapshotPointUtil.TryGetPreviousPointOnLine(point, 1);
            Assert.True(res.IsSome());
            Assert.Equal(_textBuffer.GetLine(0).Start, res.Value);
        }

        [WpfFact]
        public void TryGetPreviousPointOnLine3()
        {
            Create("foo", "bar");
            var point = _textBuffer.GetLine(0).Start;
            var res = SnapshotPointUtil.TryGetPreviousPointOnLine(point, 1);
            Assert.False(res.IsSome());
        }

        [WpfFact]
        public void GetPointsOnContainingLineFrom1()
        {
            Create("foo", "bar", "baz");
            var points = string.Concat(SnapshotPointUtil.GetPointsOnLineForward(_textBuffer.GetLine(0).Start).Select(x => x.GetChar()));
            Assert.Equal("foo", points);
        }

        [WpfFact]
        public void GetPointsOnContainingLineFrom2()
        {
            Create("foo", "bar", "baz");
            var points = string.Concat(SnapshotPointUtil.GetPointsOnLineForward(_textBuffer.GetLine(0).Start.Add(1)).Select(x => x.GetChar()));
            Assert.Equal("oo", points);
        }

        [WpfFact]
        public void GetPointsOnContainingLineBackwardsFrom1()
        {
            Create("foo", "bar", "baz");
            var points = string.Concat(SnapshotPointUtil.GetPointsOnLineBackward(_textBuffer.GetLine(0).End).Select(x => x.GetChar()));
            Assert.Equal("oof", points);
        }

        [WpfFact]
        public void GetPointsOnContainingLineBackwardsFrom2()
        {
            Create("foo", "bar", "baz");
            var points = string.Concat(SnapshotPointUtil.GetPointsOnLineBackward(_textBuffer.GetLine(1).End).Select(x => x.GetChar()));
            Assert.Equal("rab", points);
        }

        [WpfFact]
        public void GetPointsOnContainingLineBackwardsFrom3()
        {
            Create("foo", "bar", "baz");
            var points = string.Concat(SnapshotPointUtil.GetPointsOnLineBackward(_textBuffer.GetLine(1).End.Subtract(2)).Select(x => x.GetChar()));
            Assert.Equal("ab", points);
        }
    }
}
