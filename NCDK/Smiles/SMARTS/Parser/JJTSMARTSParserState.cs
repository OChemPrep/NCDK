/* Generated By:JavaCC: Do not edit this line. JJTSMARTSParserState.java Version 5.0 */
using System.Collections.Generic;

namespace NCDK.Smiles.SMARTS.Parser
{
    public class JJTSMARTSParserState
    {
        private IList<Node> nodes;
        private IList<int> marks;

        private int sp;        // number of nodes on stack
        private int mk;        // current mark
        private bool node_created;

        public JJTSMARTSParserState()
        {
            nodes = new List<Node>();
            marks = new List<int>();
            sp = 0;
            mk = 0;
        }

        /* Determines whether the current node was actually closed and
           pushed.  This should only be called in the final user action of a
           node scope.  */
        public bool NodeCreated()
        {
            return node_created;
        }

        /* Call this to reinitialize the node stack.  It is called
           automatically by the parser's ReInit() method. */
        public void Reset()
        {
            nodes.Clear();
            marks.Clear();
            sp = 0;
            mk = 0;
        }

        /* Returns the root node of the AST.  It only makes sense to call
           this after a successful parse. */
        public Node RootNode()
        {
            return nodes[0];
        }

        /* Pushes a node on to the stack. */
        public void PushNode(Node n)
        {
            nodes.Add(n);
            ++sp;
        }

        /* Returns the node on the top of the stack, and remove it from the
           stack.  */
        public Node PopNode()
        {
            if (--sp < mk)
            {
                mk = marks[marks.Count - 1];
                marks.RemoveAt(marks.Count - 1);
            }
            var ret = nodes[nodes.Count - 1];
            nodes.RemoveAt(nodes.Count - 1);
            return ret;
        }

        /* Returns the node currently on the top of the stack. */
        public Node PeekNode()
        {
            return nodes[nodes.Count - 1];
        }

        /* Returns the number of children on the stack in the current node
           scope. */
        public int NodeArity()
        {
            return sp - mk;
        }


        public void ClearNodeScope(Node n)
        {
            while (sp > mk)
            {
                PopNode();
            }
            mk = marks[marks.Count - 1];
            marks.RemoveAt(marks.Count - 1);
        }

        public void OpenNodeScope(Node n)
        {
            marks.Add(mk);
            mk = sp;
            n.JJTOpen();
        }


        /* A definite node is constructed from a specified number of
           children.  That number of nodes are popped from the stack and
           made the children of the definite node.  Then the definite node
           is pushed on to the stack. */
        public void CloseNodeScope(Node n, int num)
        {
            mk = marks[marks.Count - 1];
            marks.RemoveAt(marks.Count - 1);
            while (num-- > 0)
            {
                Node c = PopNode();
                c.JJTSetParent(n);
                n.JJTAddChild(c, num);
            }
            n.JJTClose();
            PushNode(n);
            node_created = true;
        }


        /* A conditional node is constructed if its condition is true.  All
           the nodes that have been pushed since the node was opened are
           made children of the conditional node, which is then pushed
           on to the stack.  If the condition is false the node is not
           constructed and they are left on the stack. */
        public void CloseNodeScope(Node n, bool condition)
        {
            if (condition)
            {
                int a = NodeArity();
                mk = marks[marks.Count - 1];
                marks.RemoveAt(marks.Count - 1);
                while (a-- > 0)
                {
                    Node c = PopNode();
                    c.JJTSetParent(n);
                    n.JJTAddChild(c, a);
                }
                n.JJTClose();
                PushNode(n);
                node_created = true;
            }
            else
            {
                mk = marks[marks.Count - 1];
                marks.RemoveAt(marks.Count - 1);
                node_created = false;
            }
        }
    }
}
