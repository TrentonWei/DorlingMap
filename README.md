# A toolset for Dorling Map, CTS-Map and Grid Map
Four automatic algorithm for generation of DorlingMap, CTS-Map and Grid Map.

#The Code is developed to support the findings of our four submitted paper，for more details see the pdf file as: waiting for our update 

#The tools were implemented in C# on ArcGIS 10.2 software (ESRI, USA). And the tool has a separate form to set the input, output and parameters for the algorithms.

# (1)The DorlingMap for static data
-Paper infor: Wei Z, Ding S, Xu W, et al. Elastic beam algorithm for generating circular cartograms[J]. Cartography and Geographic Information Science, 2023, 50(4): 371-384.

-The principle: an elastic beam algorithm based on minimum energy principle. 
![image](https://github.com/TrentonWei/DorlingMap/blob/master/Principle-1.png)

-The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/Interface-1.png)

-The result_1:The circular cartogram generated using the proposed approach for the population of each state in the United States of America (excluding Alaska and Hawaii)
DataSet:https://github.com/TrentonWei/DorlingMap/tree/master/Experiment%20Data%20Circular%20cartogram
![image](https://github.com/TrentonWei/DorlingMap/blob/master/USA-1.png)

- The result_2: The circular cartogram generated using the proposed approach for the population of each country in North and South America. 
Dataset:
![image](https://github.com/TrentonWei/DorlingMap/blob/master/American.png)


# (2) The DorlingMap for time-varying data
- Paper infor: Wei Z, Xu W, Ding S, et al. Efficient and stable circular cartograms for time-varying data by using improved elastic beam algorithm and hierarchical optimization[J]. Journal of Visualization, 2023, 26(2): 351-365.

- The principle: an improved elastic beam algorithm with hierarchical optimization. 
![Z. Wei, W. Xu, S. Ding, S. Zhang, Y. Wang, "Efficient and Stable Circular Cartogram for Time-varying Data by Using Improved Elastic Beam Algorithm and Hierarchical Optimization", Journal of Visualization(2022)](https://link.springer.com/article/10.1007/s12650-022-00878-z)

- The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/interface-2.png)

- The result:The circular cartogram generated by using the proposed approach for the population of each state in the United States of America (excluding Alaska and Hawaii) from 1980 to 2015 (the time interval is 5 years, 8-time points).
Dataset:https://github.com/TrentonWei/DorlingMap/tree/master/Experiment%20Data%20for%20Efficiency%20and%20Stable%20Circular%20Cartogram
![image](https://github.com/TrentonWei/DorlingMap/blob/master/USA-2.png)

# (3) The Central Time⁃Space Map 
- Paper infor: Wei Z, Liu Y, Xu W, et al. Central Time-Space Map Construction Using the Snake Model[J]. Geomatics and Information Science of Wuhan University, 2022, 47(12): 2105-2112.

- The principle: Central Time⁃Space Map Construction Using the Snake Model. 
![Z. Wei, Y. Liu, W. Xu, "Central Time-space Map Construction by Using the Snake Model(基于Snake移位方法的中心型地图构建方法)", Geomatics and Information Science of Wuhan University(武汉大学学报信息科学版)(2022)](https://mp.weixin.qq.com/s/9_4TyPiRh_qR52JWNgkuCQ)

- The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/interface-3.png)

- The result_1:The Central Time⁃Space Map Visualizing the Railway Travel Time from Wuhan to Other cities.
Dataset:https://github.com/TrentonWei/DorlingMap/tree/master/Experiment%20Data%20for%20Efficiency%20and%20Stable%20Circular%20Cartogram
![image](https://github.com/TrentonWei/DorlingMap/blob/master/CTMap.tif)

# (4) The Grid Map 
- Paper infor: Wei Z, Yang N, Xu W, et al. Generating grid maps via the snake model[J]. Transaction in GIS, 2024, 1-20.

- The interface
![image](https://github.com/TrentonWei/DorlingMap/blob/master/interface-4.png)

- The result:The Grid Map Visualizing the Central China.
![image](https://github.com/TrentonWei/DorlingMap/blob/master/GridMap.tif)


