# refer: https://www.zhihu.com/question/508800895
from scipy.optimize import linear_sum_assignment
import numpy as np

''' example
example_cost_Matrix = np.array([
   [10, 15, 9],
   [9, 18, 5],
   [6, 14, 3]])
example_matches = linear_sum_assignment(example_cost_Matrix)
print(example_matches)
'''

# 将路径替换为实际的文件路径
file_path = "C:/Users/10988.TRENTON/Desktop/Weights.txt"

'''
with open(file_path, 'r') as file:
    content = file.read()
print(content)
'''

Cost_Matrix = np.loadtxt(file_path)
print(Cost_Matrix)
matches = linear_sum_assignment(Cost_Matrix)
print(matches)




