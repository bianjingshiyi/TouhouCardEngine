using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class Flow
    {
        #region 公有方法
        #region 构造方法
        public Flow(FlowEnv env)
        {
            nodeStack = new Stack<Node>();
            scopeStack = new Stack<FlowScope>();
            scopeStack.Push(new FlowScope());
            rootScope = currentScope;
            this.env = env;
        }
        public Flow(Flow parent) : this(parent.env)
        {
            this.parent = parent;
        }
        public Flow(IGame game, ICard card, IBuff buff, IEventArg eventArg) : this(new FlowEnv(game, card, buff, eventArg))
        {
        }
        #endregion
        public FlowScope EnterScope()
        {
            var scope = new FlowScope(currentScope);
            scopeStack.Push(scope);
            return scope;
        }
        public void ExitScope()
        {
            scopeStack.Pop();
        }
        public async Task<T> getValue<T>(ValueInput input, FlowScope scope = null)
        {
            var value = await getValue(input, scope);
            if (value is T result)
                return result;
            return default;
        }
        public async Task<object> getValue(ValueInput input, FlowScope scope = null)
        {
            if (scope == null)
                scope = currentScope;

            if (scope.tryGetLocalVar(input, out var value))
            {
                return value;
            }


            var output = input.getConnectedOutputPort();

            if (output != null)
            {
                return await getValue(output, scope);
            }
            return null;

        }
        public async Task<T> getValue<T>(ValueOutput output, FlowScope scope = null)
        {
            var value = await getValue(output, scope);
            if (value is T result)
                return result;
            return default;
        }
        public async Task<object> getValue(ValueOutput output, FlowScope scope = null)
        {
            if (scope == null)
                scope = currentScope;
            if (scope.tryGetLocalVar(output, out var value))
            {
                return value;
            }

            return await GetValueDelegate(output);
        }
        public void setValue(IValuePort port, object value, FlowScope scope = null)
        {
            if (scope == null)
                scope = currentScope;
            scope.setLocalVar(port, value);
        }
        public bool tryGetFlowVariable(string key, out object value)
        {
            return flowVariables.TryGetValue(key, out value);
        }
        public object getFlowVariable(string key)
        {
            if (tryGetFlowVariable(key, out var value))
            {
                return value;
            }
            return null;
        }
        public void setFlowVariable(string key, object value)
        {
            if (flowVariables.ContainsKey(key))
            {
                flowVariables[key] = value;
            }
            else
            {
                flowVariables.Add(key, value);
            }
        }

        public async Task Run(ControlInput input)
        {
            await Invoke(input);
        }
        public async Task Run(ControlOutput outputPort)
        {
            await Invoke(outputPort);
        }
        public async Task Invoke(ControlOutput port)
        {
            var input = port.getConnectedInputPort();

            if (input == null)
            {
                return;
            }
            await Invoke(input);
        }
        public async Task Invoke(ControlInput input)
        {
            var nextPort = await InvokeDelegate(input);

            if (nextPort != null)
            {
                await Invoke(nextPort);
            }
        }


        #endregion

        private Task<ControlOutput> InvokeDelegate(ControlInput port)
        {
            Node node = port.node;
            return InvokeNode(node);
        }
        private async Task<ControlOutput> InvokeNode(Node node)
        {

            if (node == null)
            {
                return null;
            }
            nodeStack.Push(node);
            try
            {
                return await node.run(this);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException targetInvocationException)
                    e = targetInvocationException.InnerException;
                env.game.logger.logError("Game", "执行动作" + node + "发生异常：" + e);
                throw e;
            }
            finally
            {
                nodeStack.Pop();
            }
        }

        private async Task<object> GetValueDelegate(ValueOutput output)
        {
            if (output == null)
                return null;
            var node = output.node;
            try
            {
                await InvokeNode(node);
                if (currentScope.tryGetLocalVar(output, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException($"获取端口“{output}”的值失败。");
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException targetInvocationException)
                    e = targetInvocationException.InnerException;
                env.game.logger.logError("Game", "获取值" + output + "发生异常：" + e);
                throw e;
            }
        }

        #region 属性字段
        public FlowScope rootScope { get; private set; }
        public Node currentNode => nodeStack.Peek();
        public FlowScope currentScope => scopeStack.Peek();
        public FlowEnv env { get; private set; }
        public Flow parent { get; private set; }
        private Stack<Node> nodeStack;
        private Stack<FlowScope> scopeStack;
        private Dictionary<string, object> flowVariables = new Dictionary<string, object>();
        #endregion
    }

    public class FlowEnv
    {
        public IGame game { get; private set; }
        public ICard card { get; private set; }
        public IBuff buff { get; private set; }
        public IEventArg eventArg { get; private set; }
        private Dictionary<string, object> arguments;
        public FlowEnv(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.eventArg = eventArg;
            arguments = new Dictionary<string, object>();
        }
        public FlowEnv(FlowEnv other)
        {
            game = other.game;
            card = other.card;
            buff = other.buff;
            eventArg = other.eventArg;
            arguments = new Dictionary<string, object>(other.arguments);
        }
        public void SetArgument(string name, object value)
        {
            if (arguments.ContainsKey(name))
            {
                arguments[name] = value;
            }
            else
            {
                arguments.Add(name, value);
            }
        }
        public T GetArgument<T>(string name)
        {
            if (arguments.TryGetValue(name, out object value) && value is T result)
            {
                return result;
            }
            return default;
        }
    }
    public class FlowScope
    {
        public FlowScope() : this(null)
        {
        }
        public FlowScope(FlowScope parentScope)
        {
            this.parentScope = parentScope;
        }
        public bool tryGetLocalVar(IValuePort port, out object value)
        {
            FlowScope scope = this;
            while (scope != null)
            {
                if (scope.localVarDict.TryGetValue(port, out value))
                    return true;
                scope = scope.parentScope;
            }
            value = null;
            return false;
        }
        public object getLocalVar(IValuePort port)
        {
            if (tryGetLocalVar(port, out object value))
            {
                return value;
            }
            return null;
        }
        public void setLocalVar(IValuePort port, object value)
        {
            if (localVarDict.ContainsKey(port))
            {
                localVarDict[port] = value;
            }
            else
            {
                localVarDict.Add(port, value);
            }
        }
        public FlowScope parentScope;
        private Dictionary<IValuePort, object> localVarDict = new Dictionary<IValuePort, object>();
    }
}
