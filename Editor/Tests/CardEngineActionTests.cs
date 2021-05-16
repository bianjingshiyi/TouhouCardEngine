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
                defineName = "IntegerBinaryOperation",
                inputs = new ActionValueRef[]
                {
                    new ActionValueRef(new ActionNode()
                    {
                        defineName = "IntegerConst",
                        consts = new object[] { 2 }
                    }),
                    new ActionValueRef(new ActionNode()
                    {
                        defineName = "IntegerConst",
                        consts = new object[] { 2 },
                    })
                },
                consts = new object[] { BinaryOperator.add },
            };
            ;
            EventArg eventArg = new EventArg();
            _ = engine.doActionAsync(null, null, eventArg, action);
            Assert.AreEqual(4, eventArg.getVar("@IntegerBinaryOperator_3_0"));
        }
    }
}