# 说明
在Python代码中调用.NET库，有两种流行的方式。一是使用IronPython，二是使用pythonnet。用IronPython和pythonnet调用Qdp时，代码写法会稍有不同。我们提供的示例代码中，iron开头的文件是可运行在IronPython下的。

# IronPython
IronPython是一个开源的Python2.7实现，它提供了和.NET Framework的紧密集成

## 安装
http://ironpython.net/

## 运行
安装后，可用ipy.exe替代python来执行python代码，如我们：
```
ipy.exe iron_bond_test.py
ipy.exe iron_options_test.py
```
# pythonnet
pythonnet基于标准的python，通过CPython调用.NET Framework库。了解pythonnet: <http://pythonnet.github.io/>

## 安装
```
pip install pythonnet
```

## 运行
和普通python程序运行一样
```
python bond_test.py
python options_test.py
```