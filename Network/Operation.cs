using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using TouhouCardEngine;
using ExcelLibrary.BinaryFileFormat;
using LiteNetLib.Utils;
using LiteNetLib;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace NitoriNetwork.Common
{
    [Serializable]
    class Operation
    {
        // [SerializeField]
        int _id;
        public int id { set => _id = value; get => _id; }
        // [SerializeField]
        string _name;
        public string name => _name;
        public Operation(string name)
        {
            _name = name;
        }
        public virtual void setCancel()
        {
            throw new NotImplementedException();
        }
    }
    [Serializable]
    class Operation<T> : Operation, IOperation
    {
        protected TaskCompletionSource<T> tcs { get; } = new TaskCompletionSource<T>();
        public Task<T> task => tcs.Task;
        public Operation(string name) : base(name)
        {
        }
        public virtual void setResult(object obj)
        {
            if (obj == null)
                tcs.SetResult(default);
            else if (obj is T t)
                tcs.SetResult(t);
            else
                throw new InvalidCastException();
        }
        public virtual void setException(Exception e)
        {
            tcs.SetException(e);
        }
        public override void setCancel()
        {
            if (tcs.Task.IsCompleted || tcs.Task.IsFaulted || tcs.Task.IsCanceled)
                return;
            tcs.SetCanceled();
        }
    }
    interface IOperation
    {
        int id { get; }
        void setResult(object obj);
        void setException(Exception e);
    }
    interface IInvokeOperation : IOperation
    {
        int pid { get; }
        RPCRequest request { get; }
    }

    class InvokeOperation : InvokeOperation<object>
    {
        public InvokeOperation(string name, int pid) : base(name, pid)
        {
        }
        public InvokeOperation(string name, int pid, RPCRequest request) : base(name, pid, request)
        {
        }

        public override void setResult(object obj)
        {
            if (obj == null)
                tcs.SetResult(default);
            else
                tcs.SetResult(obj);
        }
    }

    class InvokeOperation<T> : Operation<T>, IInvokeOperation
    {
        public int pid { get; }

        public RPCRequest request { get; }

        public InvokeOperation(string name, int pid) : base(name)
        {
            this.pid = pid;
        }
        public InvokeOperation(string name, int pid, RPCRequest request) : base(name)
        {
            this.pid = pid;
            this.request = request;
        }
    }
    class JoinOperation : Operation<int>
    {
        public JoinOperation() : base(nameof(ClientManager.join))
        {
        }
    }

    class SendOperation<T> : Operation<T>
    {
        public SendOperation() : base(nameof(ClientManager.send))
        {
        }
    }

    class OperationList: IEnumerable<Operation>
    {
        Dictionary<int, Operation> _operationList = new Dictionary<int, Operation>();
        int _lastOperationId = 0;

        public void Add(Operation operation)
        {
            operation.id = ++_lastOperationId;
            _operationList.Add(operation.id, operation);
        }

        public Operation GetOperationByID(int id)
        {
            if (_operationList.ContainsKey(id))
                return _operationList[id];
            else return null;
        }

        public void CancleAll()
        {
            foreach (var operation in _operationList)
            {
                operation.Value.setCancel();
            }
            _operationList.Clear();
        }

        public bool Remove(Operation op)
        {
            return _operationList.Remove(op.id);
        }
        public bool Remove(int id)
        {
            return _operationList.Remove(id);
        }

        public IEnumerator<Operation> GetEnumerator()
        {
            return ((IEnumerable<Operation>)_operationList.Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_operationList.Values).GetEnumerator();
        }

        public Operation this[int index]
        {
            get { return GetOperationByID(index); }
        }

        /// <summary>
        /// 设置结果并移除对应的操作
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool SetResult(OperationResult result)
        {
            Operation op = this[result.requestID];
            IOperation invoke = op as IOperation;
            if (invoke == null)
                return false;

            if (result.obj != null && result.obj is Exception e)
                invoke.setException(e);
            else
                invoke.setResult(result.obj);

            Remove(result.requestID);
            return true;
        }
    }

    public class OperationResult
    {
        public int requestID;
        public object obj;
    }

    public class RequestOperationResult : OperationResult
    {
        public int clientID;
    }
    public static class OperationResultExt
    {
        public static OperationResult ParseInvoke(NetPacketReader reader)
        {
            var result = new OperationResult();
            result.requestID = reader.GetInt();

            string typeName = reader.GetString();
            if (!string.IsNullOrEmpty(typeName))
            {
                if (TypeHelper.tryGetType(typeName, out Type objType))
                {
                    string json = reader.GetString();
                    result.obj = BsonSerializer.Deserialize(json, objType);
                }
                else
                    throw new TypeLoadException("无法识别的类型" + typeName);
            }
            else
            {
                result.obj = null;
            }

            return result;
        }

        public static void Write(this OperationResult result, NetDataWriter writer)
        {
            writer.Put(result.requestID);
            if (result.obj == null)
            {
                writer.Put(string.Empty);
            }
            else
            {
                writer.Put(result.obj.GetType().FullName);
                writer.Put(result.obj.ToJson());
            }
        }

        public static RequestOperationResult ParseRequest(NetPacketReader reader)
        {
            var result = new RequestOperationResult();
            result.requestID = reader.GetInt();
            result.clientID = reader.GetInt();

            string typeName = reader.GetString();
            if (!string.IsNullOrEmpty(typeName))
            {
                if (TypeHelper.tryGetType(typeName, out Type objType))
                {
                    string json = reader.GetString();
                    result.obj = BsonSerializer.Deserialize(json, objType);
                }
                else
                    throw new TypeLoadException("无法识别的类型" + typeName);
            }
            else
            {
                result.obj = null;
            }

            return result;
        }
    }

}
