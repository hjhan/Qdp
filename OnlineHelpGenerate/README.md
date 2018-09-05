# 生成Qdp在线帮助文档
Qdp使用Sandcastle将C#代码中的注释内容生成在线的帮助文档。

获取Sandcastle工具： https://github.com/EWSoftware/SHFB

## 如何生成最新的Qdp帮助文档

+ 确保Qdp的各项目的编译选项中，勾选了“XML文档文件”。编译时会自动将代码中的注释生成到指定的XML文件中
+ 用Sandcastle工具打开SandcastleHelp.shfbproj
+ 确保要生成帮助文件的Qdp项目所对应的XML文档已经被加入Sandcastle项目
+ 在Visibility菜单下，打开Edit API Filter窗口，选择需要被生成帮助文档的命名空间或类型
+ 在Sandcastle工具中编译，Qdp的在线帮助文档将生成在Help文件夹中