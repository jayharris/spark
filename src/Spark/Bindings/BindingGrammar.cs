﻿using System.Collections.Generic;
using System.Linq;
using Spark.Parser;

namespace Spark.Bindings
{
    // ReSharper disable InconsistentNaming
    public class BindingGrammar : CharGrammar
    {
        public BindingGrammar()
        {
            //[4]   	NameChar	   ::=   	 Letter | Digit | '.' | '-' | '_' | ':' | CombiningChar | Extender  
            var NameChar = Ch(char.IsLetterOrDigit).Or(Ch('.', '-', '_', ':'))/*.Or(CombiningChar).Or(Extener)*/;

            //[5]   	Name	   ::=   	(Letter | '_' | ':') (NameChar)*
            var Name =
                Ch(char.IsLetter).Or(Ch('_', ':')).And(Rep(NameChar))
                .Build(hit => hit.Left + new string(hit.Down.ToArray()));

            var stringPrefixReference = 
                Ch("\"@").And(Opt(Name)).And(Ch("*\""))
                .Or(Ch("'@").And(Opt(Name)).And(Ch("*'")))
                .Build(hit => (BindingNode)new BindingPrefixReference(hit.Left.Down) { AssumeStringValue = true });

            var rawPrefixReference = Ch('@').And(Opt(Name)).And(Ch('*'))
                .Build(hit => (BindingNode)new BindingPrefixReference(hit.Left.Down));

            PrefixReference = stringPrefixReference.Or(rawPrefixReference);

            var stringNameReference = Ch("\"@").And(Name).And(Ch('\"'))
                .Or(Ch("'@").And(Name).And(Ch('\'')))
                .Build(hit => (BindingNode)new BindingNameReference(hit.Left.Down) { AssumeStringValue = true });

            var rawNameReference = Ch('@').And(Name)
                .Build(hit => (BindingNode)new BindingNameReference(hit.Down));

            NameReference = stringNameReference.Or(rawNameReference);

            Literal = Rep1(Ch(ch => true).Unless(PrefixReference.Or(NameReference)))
                .Build(hit => (BindingNode)new BindingLiteral(hit));

            Nodes = Rep(PrefixReference.Or(NameReference).Or(Literal));
        }

        public ParseAction<BindingNode> PrefixReference { get; set; }
        public ParseAction<BindingNode> NameReference { get; set; }
        public ParseAction<BindingNode> Literal { get; set; }
        public ParseAction<IList<BindingNode>> Nodes { get; set; }
    }


}
