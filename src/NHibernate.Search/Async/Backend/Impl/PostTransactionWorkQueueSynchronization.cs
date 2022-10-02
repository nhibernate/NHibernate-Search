﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NHibernate.Transaction;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl
{
    using System.Threading.Tasks;
    using System.Threading;
    internal partial class PostTransactionWorkQueueSynchronization : ITransactionCompletionSynchronization
    {
        public Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            try
            {
                ExecuteBeforeTransactionCompletion();
                return Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                return Task.FromException<object>(ex);
            }
        }

        public Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            try
            {
                ExecuteAfterTransactionCompletion(success);
                return Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                return Task.FromException<object>(ex);
            }
        }

    }
}