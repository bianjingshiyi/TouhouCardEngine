using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using TouhouCardEngine;
using ExcelLibrary.BinaryFileFormat;

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
    }
}
