# F(废物)Render

算是SRP练手，写的很垃圾，好多没写完，就这样吧。。<br>
已实现部分：
1.简单实现了ibl的三件套生成 <br>
2.DisneyBRDF(基础部分)/Cook-TorranceBRDF模型。<br>
3.实时对天空盒的SH <br>
4.CSM+ESM/VSM <br>
5.depthPeeling <br>
6.SS_PR（不好用没写完)<br>
生成的三角套里，其中prefiltermap不知道怎么能写在texture2D的mipmap里并存下来，所以用了legacy的mipmap...<br>
## 使用工具生成时记得使用"PreFilterTest"这个材质球哦~~~



![Image text](https://github.com/w199753/FRender/blob/master/Image/S_1.png)
![Image text](https://github.com/w199753/FRender/blob/master/Image/S_2.png)<br>
####和Unity自带的prefiltermap相比，结果相差甚微
![Image text](https://github.com/w199753/FRender/blob/master/Image/S_3.png)<br>
![Image text](https://github.com/w199753/FRender/blob/master/Image/S_4.png)<br>
![Image text](https://github.com/w199753/FRender/blob/master/Image/S_5.png)<br>

