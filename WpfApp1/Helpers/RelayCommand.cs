using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApp1.Helpers
{   // WPF MVVM绑定
    // 这里是绑定按钮Button的处理事件 _exec按钮点了是要执行的逻辑 _can是按钮使能能不能点

    //----------------注意，这是MVVM固定代码 框架自动生成根本不用去改 了解即可---------------------//
    //================会用即可 这套代码 十年没改过 这个代码其他项目复制都能用 所以在Helpers文件夹里
    public class RelayCommand : ICommand
    {
        // Action Func 可以说是函数闭包 匿名函数 Func有返回值
        private readonly Action<object> _exec;// 执行逻辑
        private readonly Func<object, bool> _can;// 按钮使能
        // 构造函数 指定绑定
        public RelayCommand(Action<object> exec, Func<object, bool> can = null) { _exec = exec; _can = can; }
        // ICommand提供的抽象函数接口,要求实现CanExecute Execute CanExecuteChanged
        // Execute 要求实现点了执行什么
        // CanExecute 要求实现能不能点的逻辑判断
        // CanExecuteChanged 要求实现什么时候重新判断能不能点
        // CanExecuteChanged 是个变量 event关键字 add remove 是访问器 和get set 一样 value 是隐式变量
        public bool CanExecute(object p) => _can?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
        public event EventHandler CanExecuteChanged
        { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        //CommandManager.RequerySuggested全局事件 用户对软件操作后发起通知 于是所有按钮开始自查
        //具体原理深入了解对软件开发没用，这只是轮子
    }
}
