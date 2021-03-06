/* Generated By:JJTree: Do not edit this line. SimpleNode.java Version 4.3 */
/* JavaCCOptions:MULTI=true,NODE_USES_PARSER=false,VISITOR=true,TRACK_TOKENS=false,NODE_PREFIX=AST,NODE_EXTENDS=,NODE_FACTORY=,SUPPORT_CLASS_VISIBILITY_PUBLIC=true */
namespace NCDK.Smiles.SMARTS.Parser
{
    public
    class SimpleNode : Node
    {
        protected Node parent;
        protected Node[] children;
        protected int id;
        protected object value;
        protected SMARTSParser parser;

        public SimpleNode(int i)
        {
            id = i;
        }

        public SimpleNode(SMARTSParser p, int i)
            : this(i)
        {
            parser = p;
        }

        public void JJTOpen()
        {
        }

        public void JJTClose()
        {
        }

        public void JJTSetParent(Node n) { parent = n; }
        public Node JJTGetParent() { return parent; }

        public void JJTAddChild(Node n, int i)
        {
            if (children == null)
            {
                children = new Node[i + 1];
            }
            else if (i >= children.Length)
            {
                Node[] c = new Node[i + 1];
                System.Array.Copy(children, 0, c, 0, children.Length);
                children = c;
            }
            children[i] = n;
        }

        public void JJTRemoveChild(int i)
        {
            if (i >= children.Length) return;
            Node[] c = new Node[children.Length - 1];
            System.Array.Copy(children, 0, c, 0, i);
            if (i < c.Length)
            {
                System.Array.Copy(children, i + 1, c, i, c.Length - i);
            }
            children = c;
        }

        public Node JJTGetChild(int i)
        {
            return children[i];
        }

        public int JJTGetNumChildren()
        {
            return (children == null) ? 0 : children.Length;
        }

        public void JJTSetValue(object value) { this.value = value; }
        public object JJTGetValue() { return value; }

        /// <summary>Accept the visitor. </summary>
        public virtual object JJTAccept(SMARTSParserVisitor visitor, object data)
        {
            return visitor.Visit(this, data);
        }

        /// <summary>Accept the visitor. </summary>
        public virtual object childrenAccept(SMARTSParserVisitor visitor, object data)
        {
            if (children != null)
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    children[i].JJTAccept(visitor, data);
                }
            }
            return data;
        }

        /* You can override these two methods in subclasses of SimpleNode to
           customize the way the node appears when the tree is dumped.  If
           your output uses more than one line you should override
           ToString(string), otherwise overriding ToString() is probably all
           you need to do. */

        public override string ToString() { return SMARTSParserTreeConstants.jjtNodeName[id]; }
        public virtual string ToString(string prefix) { return prefix + ToString(); }

        /* Override this method if you want to customize how the node dumps
           out its children. */

        public void Dump(string prefix)
        {
            System.Console.Out.WriteLine(ToString(prefix));
            if (children != null)
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    SimpleNode n = (SimpleNode)children[i];
                    if (n != null)
                    {
                        n.Dump(prefix + " ");
                    }
                }
            }
        }
    }
}
