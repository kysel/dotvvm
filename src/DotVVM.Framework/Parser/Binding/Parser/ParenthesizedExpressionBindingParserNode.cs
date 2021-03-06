using System.Collections.Generic;
using System.Linq;

namespace DotVVM.Framework.Parser.Binding.Parser
{
    public class ParenthesizedExpressionBindingParserNode : BindingParserNode
    {
        public BindingParserNode InnerExpression { get; private set; }

        public ParenthesizedExpressionBindingParserNode(BindingParserNode innerExpression)
        {
            InnerExpression = innerExpression;
        }

        public override IEnumerable<BindingParserNode> EnumerateNodes()
        {
            return base.EnumerateNodes().Concat(InnerExpression.EnumerateNodes());
        }
    }
}