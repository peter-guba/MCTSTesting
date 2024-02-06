SELECT
	ai_1, 
	ai_2, 
	SUM(ai_1_sym_win) as ai_1_sym_win, 
	SUM(ai_2_sym_win) as ai_2_sym_win,
	SUM(ai_1_win) as ai_1_win,
	SUM(ai_2_win) as ai_2_win,
	SUM(repeats) as iterations,
	MAX(ai_1_bs_unit_count + ai_1_d_unit_count) as "unit_count" 
FROM (
	SELECT * FROM battles WHERE ai_1_bs_unit_count + ai_1_d_unit_count = 3
	) as all_res
GROUP BY ai_1, ai_2
ORDER BY ai_1
