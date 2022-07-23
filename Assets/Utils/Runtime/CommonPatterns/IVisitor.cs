namespace App.Core.CommonPatterns
{
    public interface IElementForVisitor
    {
        void Accept(IVisitor visitor);
    }

    public interface IVisitor
    {
        // void Visit( ConcreteElementToVisit actualElement);
    }

    public class ConcreteElementToVisit : IElementForVisitor
    {
        public void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public static class IVisitorExtension
    {
        public static void Visit(this IVisitor actualVisitor, ConcreteElementToVisit element)
        {
            // all logic here. In one place
        }
    }
}