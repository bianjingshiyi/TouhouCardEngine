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
            scopeStack.Push(FlowScopePool.take());
            rootScope = currentScope;
            this.env = env;
        }
        public Flow(Flow parent, FlowEnv env) : this(env)
        {
            this.parent = parent;
        }
        public Flow(Flow parent) : this(parent, parent.env)
        {
        }
        public Flow(IGame game, ICard card, IBuff buff, IEventArg eventArg, IEffect effect) : this(new FlowEnv(game, card, buff, eventArg, effect))
        {
        }
        #endregion
        public FlowScope EnterScope()
        {
            var scope = FlowScopePool.take();
            scope.parentScope = currentScope;
            scopeStack.Push(scope);
            return scope;
        }
        public void ExitScope()
        {
            var scope = scopeStack.Pop();
            FlowScopePool.put(scope);
        }
        public async Task<T> getValue<T>(ValueInput input, FlowScope scope = null)
        {

            if (scope == null)
                scope = currentScope;

            if (scope.tryGetLocalVar(input, out object value))
            {
                if (tryConvertTo(value, out T result))
                {
                    return result;
                }
                return default;
            }


            var output = input.getConnectedOutputPort();
            var gotResult = await getValue<T>(output, scope);
            scope.setLocalVar(input, gotResult);
            return gotResult;
        }
        public Task<object> getValue(ValueInput input, FlowScope scope = null)
        {
            return getValue<object>(input, scope);
        }
        public Task<T> getValue<T>(ValueOutput output, FlowScope scope = null)
        {
            if (scope == null)
                scope = currentScope;
            if (output == null)
                return Task.FromResult<T>(default);
            if (scope.tryGetLocalVar(output, out object value))
            {
                if (tryConvertTo(value, out T result))
                {
                    return Task.FromResult(result);
                }
                return Task.FromResult<T>(default);
            }

            return GetValueDelegate<T>(output);
        }
        public Task<object> getValue(ValueOutput output, FlowScope scope = null)
        {
            return getValue<object>(output, scope);
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

        public Task Run(ControlInput input)
        {
            return Invoke(input);
        }
        public Task Run(ControlOutput outputPort)
        {
            return Invoke(outputPort);
        }
        public Task Invoke(ControlOutput port)
        {
            var input = port.getConnectedInputPort();

            if (input == null)
            {
                return Task.CompletedTask;
            }
            return Invoke(input);
        }
        public async Task Invoke(ControlInput input)
        {
            var nextPort = await InvokeDelegate(input);

            if (nextPort != null)
            {
                await Invoke(nextPort);
            }
        }

        #region 类型转换
        public static void addConversion(ParamConversion conversion)
        {
            _conversions.Add(conversion);
        }
        public static bool removeConversion(ParamConversion conversion)
        {
            return _conversions.Remove(conversion);
        }
        public static bool canConvertTo(Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
            {
                return true;
            }
            foreach (ParamConversion conv in _conversions)
            {
                if (conv == null)
                    continue;
                if (conv.canConvert(from, to))
                {
                    return true;
                }
            }
            return false;
        }
        public object convertTo(object input, Type to)
        {
            if (input == null)
                return null;
            Type from = input.GetType();
            if (to.IsAssignableFrom(from))
            {
                return input;
            }
            foreach (ParamConversion conv in _conversions)
            {
                if (conv == null)
                    continue;
                if (conv.canConvert(input.GetType(), to))
                {
                    return conv.convert(this, input, to);
                }
            }
            return input;
        }
        public bool tryConvertTo(object input, Type to, out object output)
        {
            output = default;
            if (input == null)
            {
                return false;
            }
            Type from = input.GetType();
            if (to.IsAssignableFrom(from))
            {
                output = input;
                return true;
            }
            foreach (ParamConversion conv in _conversions)
            {
                if (conv == null)
                    continue;
                if (conv.canConvert(input.GetType(), to))
                {
                    output = conv.convert(this, input, to);
                    return true;
                }
            }
            return false;
        }
        public bool tryConvertTo<T>(object input, out T output)
        {
            output = default;
            if (input == null)
            {
                return false;
            }
            if (input is T result)
            {
                output = result;
                return true;
            }
            Type to = typeof(T);
            foreach (ParamConversion conv in _conversions)
            {
                if (conv == null)
                    continue;
                if (conv.canConvert(input.GetType(), to))
                {
                    output = (T)conv.convert(this, input, to);
                    return true;
                }
            }
            return false;
        }
        #endregion
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

        private async Task<T> GetValueDelegate<T>(ValueOutput output)
        {
            if (output == null)
                return default;
            var node = output.node;
            try
            {
                await InvokeNode(node);
                if (currentScope.tryGetLocalVar(output, out var value))
                {
                    if (tryConvertTo<T>(value, out var result))
                        return result;
                    return default;
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
        private static List<ParamConversion> _conversions = new List<ParamConversion>();
        #endregion
    }

    public class FlowEnv
    {
        public IGame game { get; private set; }
        public ICard card { get; private set; }
        public IBuff buff { get; private set; }
        public IEventArg eventArg { get; private set; }
        public IEffect effect { get; private set; }
        private Dictionary<string, object> arguments;
        public FlowEnv(IGame game, ICard card, IBuff buff, IEventArg eventArg, IEffect effect)
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.eventArg = eventArg;
            this.effect = effect;
            arguments = new Dictionary<string, object>();
        }
        public FlowEnv(FlowEnv other)
        {
            game = other.game;
            card = other.card;
            buff = other.buff;
            eventArg = other.eventArg;
            effect = other.effect;
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
        public object GetArgument(string name)
        {
            if (arguments.TryGetValue(name, out object value))
            {
                return value;
            }
            return default;
        }
    }
    public class FlowScope
    {
        public FlowScope()
        {
        }
        public void reset()
        {
            parentScope = null;
            localVarDict.Clear();
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
    public static class FlowScopePool
    {
        public static FlowScope take()
        {
            if (_scopePool.Count <= 0)
            {
                return new FlowScope();
            }
            else
            {
                var scope = _scopePool[0];
                _scopePool.RemoveAt(0);
                return scope;
            }
        }
        public static void put(FlowScope scope)
        {
            scope.reset();
            _scopePool.Add(scope);
        }
        static List<FlowScope> _scopePool = new List<FlowScope>();
    }
}
