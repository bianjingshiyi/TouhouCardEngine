using NUnit.Framework;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;

namespace Tests
{
    public class CardEngineActionTests
    {
        /// <summary>
        /// case ZMCS-1167 节点的输出值连接多个输入值，会导致无法正确获取到值并报错
        /// </summary>
        [Test]
        public void multipleInputRefToOutputThroughScopeTest()
        {
            CardEngine game = new CardEngine();
            var opDefine = new IntegerOperationActionDefine();
            var constDefine = new IntegerConstActionDefine();
            game.addActionDefine("IntegerBinaryOperation", opDefine);
            game.addActionDefine("IntegerConst", constDefine);

            ActionGraph graph = new ActionGraph();
            //动作节点1的输出值会被多次引用，获取节点的返回值应该可以得到正确答案。
            ActionNode actionNode1 = graph.createActionNode(constDefine);
            actionNode1.setConst("value", 2);
            var constOutput = actionNode1.getOutputPort<ValueOutput>("result");

            Assert.AreEqual(2, game.getValue<int>(new Flow(game, null, null, null), constOutput).Result);



            ActionNode actionNode2 = graph.createActionNode(opDefine);
            actionNode2.setConst("operator", IntegerOperator.add);

            graph.connect(constOutput, actionNode2.getParamInputPort("Value", 0));
            graph.connect(constOutput, actionNode2.getParamInputPort("Value", 1));
            var intOpOutput = actionNode2.getOutputPort<ValueOutput>("result");

            Assert.AreEqual(4, game.getValue<int>(new Flow(game, null, null, null), intOpOutput).Result);



            ActionNode actionNode3 = graph.createActionNode(opDefine);
            actionNode3.setConst("operator", IntegerOperator.sub);

            graph.connect(intOpOutput, actionNode2.getParamInputPort("Value", 0));
            graph.connect(constOutput, actionNode2.getParamInputPort("Value", 1));
            var intOp2Output = actionNode3.getOutputPort<ValueOutput>("result");

            Assert.AreEqual(2, game.getValue<int>(new Flow(game, null, null, null), intOp2Output).Result);
        }
        [Test]
        public void doActionWithGeneratedActionDefineTest()
        {
            CardEngine game = new CardEngine();

            ActionGraph genGraph = new ActionGraph();

            var generated = new GeneratedActionDefine(genGraph, 1, null, "GeneratedActionDefine",
                new PortDefine[]
                {
                    PortDefine.Value(typeof(int),"A"),
                    PortDefine.Value(typeof(int),"B")
                },
                new PortDefine[]
                {
                    PortDefine.Const(typeof(int),"C")
                },
                new PortDefine[]
                {
                    PortDefine.Value(typeof(int),"Result")
                }
                );
            generated.InitNodes();

            var opDefine = new IntegerOperationActionDefine();
            var constDefine = new IntegerConstActionDefine();
            game.addActionDefine("IntegerBinaryOperation", opDefine);
            game.addActionDefine("IntegerConst", constDefine);
            game.addActionDefine("GeneratedActionDefine", generated);


            ActionNode actionNode1 = genGraph.createActionNode(opDefine, 0, 0);
            actionNode1.setConst("operator", IntegerOperator.add);

            ActionNode actionNode2 = genGraph.createActionNode(constDefine, 100, 0);
            actionNode1.setConst("operator", IntegerOperator.mul);

            var entryNode = generated.getEntryNode();
            var exitNode = generated.getEntryNode();

            genGraph.connect(entryNode.getExitPort(), actionNode1.getEnterPort());
            genGraph.connect(actionNode1.getExitPort(), actionNode2.getExitPort());
            genGraph.connect(actionNode2.getExitPort(), exitNode.getInputPort("Result"));


            ActionGraph graph = new ActionGraph();
            ActionNode action = graph.createActionNode(generated, 0, 0);

            ActionNode constNode1 = graph.createActionNode(constDefine, -100, 0);
            constNode1.setConst("Value", 2);

            ActionNode constNode2 = graph.createActionNode(constDefine, -100, 100);
            constNode2.setConst("Value", 2);

            action.setConst("C", 2);
            var task = game.getValue(new Flow(game, null, null, null), action.getOutputPort<ValueOutput>("Result"));
            Assert.AreEqual(8, task.Result);
        }
        [Test]
        public void doActionTest()
        {
            CardEngine engine = new CardEngine();
            var opDefine = new IntegerOperationActionDefine();
            var constDefine = new IntegerConstActionDefine();
            engine.addActionDefine("IntegerBinaryOperation", opDefine);
            engine.addActionDefine("IntegerConst", constDefine);
            ActionGraph graph = new ActionGraph();
            ActionNode action = graph.createActionNode(opDefine);
            ActionNode const1 = graph.createActionNode(constDefine);
            ActionNode const2 = graph.createActionNode(constDefine);
            action.setConst("operator", IntegerOperator.add);
            const1.setConst("value", 2);
            const1.setConst("value", 2);
            int result = engine.getValue<int>(new Flow(engine, null, null, null), action.getOutputPort<ValueOutput>("result")).Result;
            Assert.AreEqual(4, result);
        }
    }
}