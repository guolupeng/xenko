// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Services
{
    /// <summary>
    /// 这个接口允许在创建代码的线程(通常是主线程)中分派部分代码的执行This interface allows to dispatch execution of a portion of code in the thread where it was created, usually the Main thread.
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// 在dispatcher线程中执行给定的回调。此方法将阻塞，直到完成回调的执行Executes the given callback in the dispatcher thread. This method will block until the execution of the callback is completed.
        /// </summary>
        /// <param name="callback">The callback to execute in the dispatcher thread.</param>
        void Invoke(Action callback);

        /// <summary>
        /// 在dispatcher线程中执行给定的回调。此方法将阻塞，直到完成回调的执行Executes the given callback in the dispatcher thread. This method will block until the execution of the callback is completed.
        /// </summary>
        /// <typeparam name="TResult">回调函数返回的结果的类型The type of result returned by the callback.</typeparam>
        /// <param name="callback">要在dispatcher线程中执行的回调The callback to execute in the dispatcher thread.</param>
        /// <returns>执行回调返回的结果The result returned by the executed callback.</returns>
        TResult Invoke<TResult>(Func<TResult> callback);

        /// <summary>
        /// 在dispatcher线程中执行给定的异步函数。此方法将异步运行并立即返回Executes the given asynchronous function in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <param name="callback">The asynchronous function to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given function.</returns>
        [NotNull]
        Task InvokeAsync(Action callback);

        /// <summary>
        /// 在dispatcher线程中执行给定的异步函数。此方法将异步运行并立即返回Executes the given asynchronous function in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <param name="callback">The asynchronous function to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given function.</returns>
        /// <remarks>This method uses a low priority to schedule the action on the dispatcher thread.</remarks>
        [NotNull]
        Task LowPriorityInvokeAsync(Action callback);

        /// <summary>
        /// Executes the given asynchronous function in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the task.</typeparam>
        /// <param name="callback">The asynchronous function to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given task.</returns>
        [NotNull]
        Task<TResult> InvokeAsync<TResult>(Func<TResult> callback);

        /// <summary>
        /// 在dispatcher线程中执行给定的异步任务。此方法将异步运行并立即返回Executes the given asynchronous task in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <param name="task">The asynchronous task to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given function.</returns>
        [NotNull]
        Task InvokeTask(Func<Task> task);

        /// <summary>
        /// Executes the given asynchronous task in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the task.</typeparam>
        /// <param name="task">The asynchronous task to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given task.</returns>
        [NotNull]
        Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task);

        /// <summary>
        /// 验证当前线程是否为dispatcher线程Verifies that the current thread is the dispatcher thread.
        /// </summary>
        /// <returns><c>True</c> if the current thread is the dispatcher thread, <c>False</c> otherwise.</returns>
        bool CheckAccess();

        /// <summary>
        /// 确保当前线程是(或不是)dispatcher线程。如果不是这样，这个方法将抛出异常Ensures that the current thread is (or is not) the dispatcher thread. This method will throw an exception if it is not the case.
        /// </summary>
        void EnsureAccess(bool inDispatcherThread = true);
    }
}
