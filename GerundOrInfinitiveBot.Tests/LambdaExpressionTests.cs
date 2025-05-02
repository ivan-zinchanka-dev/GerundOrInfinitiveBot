using System.Linq.Expressions;

namespace GerundOrInfinitiveBot.Tests;

[TestFixture]
public class LambdaExpressionTests
{
    [Test]
    public void TestLambdaExpression()
    {
        Expression<Func<int, int>> expression = x => x * 5;
        Assert.That(expression.Body.ToString(), Is.EqualTo("(x * 5)"));
        Assert.That(expression.Body.NodeType, Is.EqualTo(ExpressionType.Multiply));
        
        var binaryExp = (BinaryExpression)expression.Body;
        Assert.That(binaryExp.Left.ToString(), Is.EqualTo("x"));
        Assert.That(binaryExp.Right.ToString(), Is.EqualTo("5"));
    }
}