using UnityEngine;
using System.Collections.Generic; 
using System.Collections;

public class ObjectPoolManager : IEnumerable, System.IDisposable {

    private List<GameObject> items = new List<GameObject>();
    private Queue<GameObject> queue = new Queue<GameObject>();
    private GameObject original;
    private int maxCount;

    public IEnumerator GetEnumerator() {
        foreach(GameObject item in items) 
             yield return item; 
    }

    public ObjectPoolManager(GameObject original, int initialCount, int maxCount) { 
    
         this.original = original; 
         this.maxCount = maxCount; 
    
         for(int i = 0; i < initialCount && i< maxCount; i++){ 
             GameObject newItem = GameObject.Instantiate(original) as GameObject; 
             newItem.SetActive(false); 
             items.Add(newItem); 
             queue.Enqueue(newItem); 
         } 
    
     } 

    public GameObject TakeItem() { 
    
        if(queue.Count > 0){ 
            GameObject item = queue.Dequeue () as GameObject; 
            item.SetActive(true); 
            return item.gameObject; 
         
        }else if(items.Count < maxCount) { 
            GameObject newItem = GameObject.Instantiate(original) as GameObject; 
            items.Add(newItem); 
            return newItem; 
         
        }else{ 
            throw new UnityException("Memory Pool의 한도를 초과했습니다."); 
        } 
    }

    public void PutItem(GameObject gameObject) {

        if(gameObject == null)
            return;

        gameObject.SetActive(false);
        queue.Enqueue(gameObject);

    }

    public void ClearItem() {

        foreach(GameObject item in items)
            item.SetActive(false);
    }

    public void Dispose() {

        foreach(GameObject item in items)
            GameObject.Destroy(item);

        items.Clear();
        queue.Clear();
        items = null;
        queue = null;
    } 
}
