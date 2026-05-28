TRUNCATE TABLE [Ingredients]
TRUNCATE TABLE [IngredientCategories]
TRUNCATE TABLE [RecipeCategories]
GO
 
 INSERT INTO [RecipeCategories] ([Name]) VALUES 
(N'Неопределен'),
(N'Предястия'),
(N'Основни ястия'),
(N'Десерти'),
(N'Супи'),
(N'Салати'),
(N'Закуски'),
(N'Тестени изделия'),
(N'Напитки'),
(N'Морска кухня')

INSERT INTO [IngredientCategories] ([Name]) VALUES
(N'Добавена от потребител'),
(N'Месни продукти'),
(N'Млечни продукти'),
(N'Яйчни продукти'),
(N'Зеленчуци'),
(N'Плодове'),
(N'Зърнени и тестени'),
(N'Ядки и семена'),
(N'Подправки и билки'),
(N'Готови сосове и добавки');

-- Месни продукти
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Пилешко месо', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пилешко филе', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пилешки бутчета', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пилешки крилца', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пилешка кайма', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пилешка пържола', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),

(N'Свинско месо', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Свинско филе', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Свински ребра', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Свинска кайма', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),

(N'Телешко месо', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Телешка пържола', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Телешка кайма', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),

(N'Агнешко месо', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Агнешки котлети', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),

(N'Кайма', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Наденица', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Суджук', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Салам', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Шунка', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пастърма', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),

(N'Бекон', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Пушено месо', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти')),
(N'Луканка', (SELECT Id FROM IngredientCategories WHERE Name = N'Месни продукти'));


-- Млечни продукти
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Кашкавал', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Сирене', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Бяло саламурено сирене', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Козе сирене', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Овче сирене', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Крем сирене', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Моцарела', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Пармезан', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Гауда', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),

(N'Мляко', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Прясно мляко', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Кисело мляко', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Сметана', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Готварска сметана', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Сладкарска сметана', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),

(N'Кисело мляко с пробиотици', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Айрян', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Кефир', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),

(N'Масло', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Гхи', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти')),
(N'Извара', (SELECT Id FROM IngredientCategories WHERE Name = N'Млечни продукти'));


-- Яйчни продукти
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Яйца', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Кокоши яйца', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Пъдпъдъчи яйца', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Патешки яйца', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),

(N'Белтъци', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Жълтъци', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),

(N'Сушени яйца (прах)', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Пастьоризирани течни яйца', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти')),
(N'Яйчена смес', (SELECT Id FROM IngredientCategories WHERE Name = N'Яйчни продукти'));


-- Зеленчуци
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Домати', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Чери домати', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Краставици', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Чушки', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Червени чушки', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Зелени чушки', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Моркови', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Картофи', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Сладък картоф (батат)', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Лук', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Червен лук', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Зелен лук', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Чесън', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Броколи', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Карфиол', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Тиквички', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Патладжан', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Целина (корен)', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Целина (стъбла)', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Грах', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Царевица', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Зелен фасул', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Кисело зеле', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Прясно зеле', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Спанак', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Рукола', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Салата айсберг', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци')),
(N'Маруля', (SELECT Id FROM IngredientCategories WHERE Name = N'Зеленчуци'));


-- Плодове
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Ябълки', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Зелени ябълки', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Банани', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Круши', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Праскови', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Нектарини', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Сливи', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Кайсии', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Грозде', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Портокали', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Мандарини', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Лимони', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Лайм', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Ананас', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Киви', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Пъпеш', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Диня', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Манго', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Авокадо', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Боровинки', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Ягоди', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Малини', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Къпини', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Червени боровинки', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове')),
(N'Грейпфрут', (SELECT Id FROM IngredientCategories WHERE Name = N'Плодове'));

-- Зърнени и тестени
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Бяло брашно', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Пълнозърнесто брашно', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Овесени ядки', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Ориз', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Кафяв ориз', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Булгур', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Кус-кус', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Пшеничен грис', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Царевичен грис', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Просо', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Киноа', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),

(N'Макарони', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Спагети', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Талятели', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Фусили', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Пене', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Лазаня кори', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),

(N'Хляб', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Пълнозърнест хляб', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Питка', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Багета', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Кифлички', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Тарали', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени')),
(N'Блат за пица', (SELECT Id FROM IngredientCategories WHERE Name = N'Зърнени и тестени'));


-- Ядки и семена
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Орехи', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Орехи смлени', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Бадеми', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Бадеми филирани', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Лешници', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Фъстъци', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Фъстъчено масло', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Кашу', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Шамфъстък', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),

(N'Слънчогледови семки', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Тиквени семки', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Чиа семена', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Сусам', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Ленено семе', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Маково семе', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),

(N'Кокосови стърготини', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Кокосово брашно', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена')),
(N'Кокосово масло', (SELECT Id FROM IngredientCategories WHERE Name = N'Ядки и семена'));

-- Подправки и билки
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Сол', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Черен пипер', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Червен пипер', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Лют червен пипер', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Чубрица', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Магданоз', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Копър', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Риган', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Босилек', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Мащерка', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Розмарин', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Салвия', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),

(N'Къри', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Канела', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Кимион', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Дафинов лист', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Гарам масала', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Мента (сушена)', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),

(N'Пресен лук', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Пресен чесън', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Джинджифил (сух)', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Джинджифил (пресен)', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),

(N'Ванилия', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Мускатово орехче', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки')),
(N'Карамфил', (SELECT Id FROM IngredientCategories WHERE Name = N'Подправки и билки'));


-- Готови сосове и добавки
INSERT INTO [Ingredients] (Name, IngredientCategoryId)
VALUES 
(N'Кетчуп', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Майонеза', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Горчица', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Соев сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сладко-кисел сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Барбекю сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Чеснов сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Цезар дресинг', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сос от авокадо', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),

(N'Песто', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Хумус', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сос Тартар', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сос Бешамел', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сос Болонезе (готов)', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сос Карбонара (готов)', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),

(N'Сладко от ягоди', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сладко от боровинки', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Шоколадов топинг', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Карамелен сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),

(N'Оцет', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Балсамов оцет', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Лимонов сок (бутилка)', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки')),
(N'Сладък чили сос', (SELECT Id FROM IngredientCategories WHERE Name = N'Готови сосове и добавки'));

GO