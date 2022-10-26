---
title: Hello MarkTheRipper!
tags: foo,bar
date: 10/26/2022 21:23:23 +09:00
---

This is sample post.

## H2

H2 body.

### H3

H3 body. [MarkTheRipper is here.](https://github.com/kekyo/MarkTheRipper)

### JavaScript code block

Refer: [JavaScript by Example (published by Packt, Under MIT)](https://github.com/PacktPublishing/JavaScript-by-Example/blob/master/Chapter01/CompletedCode/src/scripts.js)

```javascript
class ToDoClass {
    constructor() {
      this.tasks = JSON.parse(localStorage.getItem('TASKS'));
      if(!this.tasks) {
        this.tasks = [
          {task: 'Go to Dentist', isComplete: false},
          {task: 'Do Gardening', isComplete: true},
          {task: 'Renew Library Account', isComplete: false},
        ];
      }

      this.loadTasks();
      this.addEventListeners();
    }
}
```
