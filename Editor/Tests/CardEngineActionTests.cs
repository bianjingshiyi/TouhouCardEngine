using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine;

namespace Tests
{
    public class CardEngineActionTests
    {
        [Test]
        public void doActionTest()
        {
            CardEngine engine = new CardEngine();
            engine.addActionDefine("IntegerBinaryOperation", new IntegerBinaryOperationActionDefine());
            engine.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            ActionNode action = new ActionNode()
            {
                defineName = "IntegerConst",
                consts = new object[] { 2 },
                outputs = new ActionVarRef[1]
                {
                    new ActionVarRef()
                    {
                        index = 0,
                        varName = "@IntegerConst_1_0"
                    }
                },
                next = new ActionNode()
                {
                    defineName = "IntegerConst",
                    consts = new object[] { 2 },
                    outputs = new ActionVarRef[1]
                    {
                        new ActionVarRef()
                        {
                            index = 0,
                            varName = "@IntegerConst_2_0"
                        }
                    },
                    next = new ActionNode()
                    {
                        defineName = "IntegerBinaryOperation",
                        inputs = new ActionVarRef[2]
                        {
                            new ActionVarRef() { index = 0, varName = "@IntegerConst_1_0" },
                            new ActionVarRef() { index = 1, varName = "@IntegerConst_2_0" }
                        },
                        consts = new object[] { BinaryOperator.add },
                        outputs = new ActionVarRef[1]
                        {
                            new ActionVarRef()
                            {
                                index = 0,
                                varName = "@IntegerBinaryOperator_3_0"
                            }
                        }
                    }
                },
            };
            EventArg eventArg = new EventArg();
            _ = engine.doAction(null, null, eventArg, action);
            Assert.AreEqual(4, eventArg.getVar("@IntegerBinaryOperator_3_0"));
        }
    }
}