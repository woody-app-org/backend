package com.woody.backend.infraestructure.entity.embeddedIds;

import jakarta.persistence.Embeddable;

@Embeddable
public class LikeId {
    private long userId;
    private long postId;

    public LikeId() {}
    
    public LikeId(long userId, long postId) {
        this.userId = userId;
        this.postId = postId;
    }

    public long getUserId() {
        return userId;
    }

    public void setUserId(long userId) {
        this.userId = userId;
    }

    public long getPostId() {
        return postId;
    }

    public void setPostId(long postId) {
        this.postId = postId;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof LikeId that)) return false;
        return userId == that.userId && postId == that.postId;
    }

    @Override
    public int hashCode() {
        return Long.hashCode(userId) + Long.hashCode(postId);
    }
}
