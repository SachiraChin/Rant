﻿using System;
using System.Collections.Generic;

using Manhood.Compiler;

using Stringes;
using Stringes.Tokens;

namespace Manhood
{
    internal partial class Interpreter
    {
		/// <summary>
		/// Represents an outline for an action that is queued by a state object and executed immediately before parsing any tokens in the state.
		/// </summary>
        internal abstract class Blueprint
        {
            protected readonly Interpreter I;

            protected Blueprint(Interpreter interpreter)
            {
	            I = interpreter;
            }

            public abstract bool Use();
        }

        internal sealed class TagBlueprint : Blueprint
        {
            public Source Source { get; private set; }
            public Stringe Name { get; private set; }
            public int ParameterCount { get; private set; }

            public TagBlueprint(Interpreter interpreter, Source source, Stringe name, int paramCount) : base(interpreter)
            {
                Source = source;
                Name = name;
                ParameterCount = paramCount;
            }

            public override bool Use()
            {
                var args = new string[ParameterCount];
                for (int i = 0; i < ParameterCount; i++)
                {
                    args[i] = I._resultStack.Pop().MainOutput;
                }

                TagFunc func;

                if (!TagFuncs.TryGetValue(Name.Value.ToLower(), out func))
                {
                    throw new ManhoodException(Source, Name, "The tag '" + Name.Value + "' does not exist.");
                }

                func.Invoke(I, Source, Name, args);

                return false;
            }
        }

        internal sealed class RepeaterBlueprint : Blueprint
        {
            private readonly Repeater _repeater;
            private readonly IEnumerable<Token<TokenType>>[] _items; 

            public RepeaterBlueprint(Interpreter interpreter, Repeater repeater, IEnumerable<Token<TokenType>>[] items) : base(interpreter)
            {
                _repeater = repeater;
                _items = items;
            }

            // TODO: Add support for synchronizers
            // TODO: Consider storing repeaters in a public stack to allow access to [first], [last], etc.
            // TODO: Move item list to Repeater class along with the BlockAttribs it was created from
            public override bool Use()
            {
                if (_repeater.Finished) return false;
                _repeater.Next();
                I.CurrentState.AddBlueprint(this);
                I.PushState(State.CreateDerivedDistinct(I.CurrentState.Reader.Source, _items[I._rng.Next(_items.Length)], I, I.CurrentState.Output));
                return true;
            }
        }

        internal sealed class MetapatternBlueprint : Blueprint
        {
            public MetapatternBlueprint(Interpreter interpreter) : base(interpreter)
            {
            }

            public override bool Use()
            {
                var srcstr = I.PopResult();
                var src = new Source("Meta_" + String.Format("{0:X16}", srcstr.Hash()), SourceType.Metapattern, srcstr);
                I.PushState(State.Create(src, I));
                return true;
            }
        }
    }
}